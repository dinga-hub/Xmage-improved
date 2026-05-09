using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

/// <summary>
/// XMage AI Patch Installer
/// - Auto-detects XMage installation via registry + common paths
/// - Detects versioned JAR filenames already on disk (any XMage server version)
/// - Downloads 3 unversioned JARs from GitHub Releases, writes using those exact names
/// - Patches startServer.bat + installed.properties (if found) for JVM heap / G1GC
/// </summary>
class XMageInstaller
{
    const string BASE_URL = "https://github.com/dinga-hub/Xmage-improved/releases/latest/download";
    const long   MIN_JAR_BYTES = 10240; // 10 KB sanity check

    static int Main(string[] args)
    {
        Console.Title = "XMage AI Patch - Instalador";

        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls12 |
            System.Net.SecurityProtocolType.Tls11;

        Header();

        string serverDir = FindXMageServer();

        if (serverDir != null)
        {
            Console.WriteLine("XMage encontrado em: " + serverDir);
            Console.WriteLine();
            Console.Write("Usar este caminho? [S/N]: ");
            string ans = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            if (ans == "N") serverDir = null;
        }

        if (serverDir == null)
        {
            serverDir = AskUserForPath();
            if (serverDir == null) { Pause(); return 1; }
        }

        string libDir     = Path.Combine(serverDir, "lib");
        string pluginsDir = Path.Combine(serverDir, "plugins");

        string jarAi    = DetectJar(libDir,     "mage-player-ai-*.jar");
        string jarAiMa  = DetectJar(pluginsDir, "mage-player-ai-ma-*.jar");
        string jarHuman = DetectJar(pluginsDir, "mage-player-human-*.jar");

        if (jarAi == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: nao encontrei mage-player-ai-*.jar em lib\\.");
            Console.WriteLine("       Instale/atualize o XMage (servidor) pelo launcher oficial pelo menos uma vez.");
            Pause(); return 1;
        }
        if (jarAiMa == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: nao encontrei mage-player-ai-ma-*.jar em plugins\\.");
            Pause(); return 1;
        }
        if (jarHuman == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: nao encontrei mage-player-human-*.jar em plugins\\.");
            Pause(); return 1;
        }

        Console.WriteLine();
        Console.WriteLine("JARs detectados (independentes da versao do servidor):");
        Console.WriteLine("  lib\\"     + jarAi);
        Console.WriteLine("  plugins\\" + jarAiMa);
        Console.WriteLine("  plugins\\" + jarHuman);
        Console.WriteLine();

        bool ok = true;
        ok = ok && DownloadJar("mage-player-ai.jar",    Path.Combine(libDir,     jarAi),    1, 3);
        ok = ok && DownloadJar("mage-player-ai-ma.jar", Path.Combine(pluginsDir, jarAiMa),  2, 3);
        ok = ok && DownloadJar("mage-player-human.jar", Path.Combine(pluginsDir, jarHuman), 3, 3);

        if (!ok)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: instalacao falhou. Verifique sua conexao e tente novamente.");
            Pause(); return 1;
        }

        PatchJvmAndMemoryFiles(serverDir);

        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine(" Patch instalado com sucesso!");
        Console.WriteLine(" Reinicie o servidor XMage para aplicar.");
        Console.WriteLine("============================================");
        Pause();
        return 0;
    }

    static string FindXMageServer()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\XMage"))
            {
                if (key != null)
                {
                    string d = key.GetValue("InstallDir") as string;
                    if (!string.IsNullOrEmpty(d)) { string f = TryServerDir(d); if (f != null) return f; }
                }
            }
        }
        catch { /* ignore */ }

        string[] basePaths =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
            @"C:\",
        };

        foreach (string b in basePaths)
        {
            foreach (string name in new[] { "XMage", "xmage" })
            {
                string f = TryServerDir(Path.Combine(b, name));
                if (f != null) return f;
            }
        }

        return null;
    }

    static string TryServerDir(string basePath)
    {
        if (string.IsNullOrEmpty(basePath)) return null;

        if (Directory.Exists(Path.Combine(basePath, "lib"))) return basePath;

        string s1 = Path.Combine(basePath, "mage-server");
        if (Directory.Exists(Path.Combine(s1, "lib"))) return s1;

        string s2 = Path.Combine(basePath, "xmage", "mage-server");
        if (Directory.Exists(Path.Combine(s2, "lib"))) return s2;

        return null;
    }

    static string AskUserForPath()
    {
        Console.WriteLine("Nao foi possivel encontrar o XMage automaticamente.");
        Console.WriteLine();
        Console.WriteLine("Informe o caminho da pasta mage-server. Exemplos:");
        Console.WriteLine(@"  C:\Users\SeuNome\AppData\Roaming\XMage\mage-server");
        Console.WriteLine(@"  C:\Users\SeuNome\Desktop\XMage\xmage\mage-server");
        Console.WriteLine();
        Console.Write("Caminho: ");
        string input = (Console.ReadLine() ?? "").Trim().Trim('"');

        if (string.IsNullOrEmpty(input)) { Console.WriteLine("Nenhum caminho informado."); return null; }

        string found = TryServerDir(input);
        if (found != null) return found;

        Console.WriteLine("ERRO: nao encontrei lib\\ em: " + input);
        return null;
    }

    /// <summary>
    /// Picks the versioned JAR name already present. If several match (rare), uses newest LastWriteTime.
    /// Excludes *.backup. Returns null if none — caller must abort (do not guess a version string).
    /// </summary>
    static string DetectJar(string dir, string pattern)
    {
        if (!Directory.Exists(dir)) return null;

        string[] paths = Directory.GetFiles(dir, pattern);
        var candidates = paths
            .Where(p =>
            {
                string n = Path.GetFileName(p);
                return !n.EndsWith(".backup", StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(p => new FileInfo(p).LastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0) return null;
        return Path.GetFileName(candidates[0]);
    }

    static bool DownloadJar(string jarName, string destPath, int step, int total)
    {
        Console.Write("[" + step + "/" + total + "] Baixando " + jarName + "... ");

        string backupPath = destPath + ".backup";
        try
        {
            if (File.Exists(destPath))
                File.Copy(destPath, backupPath, overwrite: true);

            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "XMageAIPatch/1.0";
                wc.DownloadFile(BASE_URL + "/" + jarName, destPath);
            }

            long size = new FileInfo(destPath).Length;
            if (size < MIN_JAR_BYTES)
            {
                Console.WriteLine("ERRO: arquivo muito pequeno (" + size + " bytes) — download pode ter falhado.");
                RestoreBackup(backupPath, destPath);
                return false;
            }

            Console.WriteLine("OK (" + (size / 1024) + " KB)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERRO: " + ex.Message);
            RestoreBackup(backupPath, destPath);
            return false;
        }
    }

    static void RestoreBackup(string backupPath, string destPath)
    {
        try { if (File.Exists(backupPath)) File.Copy(backupPath, destPath, overwrite: true); }
        catch { /* best-effort */ }
    }

    static void PatchJvmAndMemoryFiles(string serverDir)
    {
        Console.WriteLine("[4/4] Memoria JVM (-Xmx4096m + G1GC)");

        string batPath = FindStartServerBat(serverDir);
        if (batPath != null)
            PatchOneJvmFile(batPath);
        else
            Console.WriteLine("  AVISO: startServer.bat nao encontrado.");

        string props = FindInstalledProperties(serverDir);
        if (props != null)
            PatchOneJvmFile(props);

        if (batPath == null && props == null)
            Console.WriteLine("  AVISO: nenhum arquivo de memoria encontrado; ajuste manualmente se precisar.");
    }

    static void PatchOneJvmFile(string path)
    {
        string label = Path.GetFileName(path);
        string content = File.ReadAllText(path);

        if (content.Contains("-Xmx4096m") && content.Contains("UseG1GC"))
        {
            Console.WriteLine();
            Console.WriteLine("  [OK] " + label);
            return;
        }

        content = Regex.Replace(content, @"-Xmx\S+", "-Xmx4096m");
        if (!content.Contains("UseG1GC"))
            content = content.Replace("java ", "java -XX:+UseG1GC ");

        File.WriteAllText(path, content);
        Console.WriteLine();
        Console.WriteLine("  [ATUALIZADO] " + path);
    }

    static string FindInstalledProperties(string serverDir)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(serverDir);
            for (int i = 0; i < 5 && dir != null; i++, dir = dir.Parent)
            {
                string p = Path.Combine(dir.FullName, "installed.properties");
                if (File.Exists(p)) return p;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    static string FindStartServerBat(string serverDir)
    {
        string[] candidates =
        {
            Path.Combine(serverDir, "startServer.bat"),
            Path.Combine(serverDir, "..", "startServer.bat"),
            Path.Combine(serverDir, "..", "xmage", "startServer.bat"),
        };
        foreach (string c in candidates)
        {
            string full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    static void Header()
    {
        Console.WriteLine("============================================");
        Console.WriteLine(" XMage AI Patch - Instalador");
        Console.WriteLine(" Commander AI melhorado por Diego");
        Console.WriteLine("============================================");
        Console.WriteLine();
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para fechar...");
        try { Console.ReadKey(intercept: true); } catch { }
    }
}
