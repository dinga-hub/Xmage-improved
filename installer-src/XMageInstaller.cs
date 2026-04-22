using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

/// <summary>
/// XMage AI Patch Installer
/// - Auto-detects XMage installation via registry + common paths
/// - Detects exact versioned JAR filenames (e.g. mage-player-ai-1.4.58.jar)
/// - Downloads 3 JARs from GitHub Releases using WebClient (follows redirects)
/// - Validates file size after download
/// - Patches startServer.bat with -Xmx4096m and G1GC
/// </summary>
class XMageInstaller
{
    const string BASE_URL = "https://github.com/dinga-hub/Xmage-improved/releases/latest/download";
    const long   MIN_JAR_BYTES = 10240; // 10 KB sanity check

    static int Main(string[] args)
    {
        Console.Title = "XMage AI Patch - Instalador";

        // .NET 4.0 default is TLS 1.0 — GitHub requires TLS 1.2+
        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls12 |
            System.Net.SecurityProtocolType.Tls11;

        Header();

        // ── 1. Locate mage-server ────────────────────────────────────────────
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

        // ── 2. Detect versioned JAR names ────────────────────────────────────
        string jarAi    = DetectJar(libDir,     "mage-player-ai-*.jar",    "mage-player-ai-1.4.58.jar");
        string jarAiMa  = DetectJar(pluginsDir, "mage-player-ai-ma-*.jar", "mage-player-ai-ma-1.4.58.jar");
        string jarHuman = DetectJar(pluginsDir, "mage-player-human-*.jar", "mage-player-human-1.4.58.jar");

        Console.WriteLine();
        Console.WriteLine("Versao detectada : " + jarAi);
        Console.WriteLine("Destinos:");
        Console.WriteLine("  lib\\"     + jarAi);
        Console.WriteLine("  plugins\\" + jarAiMa);
        Console.WriteLine("  plugins\\" + jarHuman);
        Console.WriteLine();

        // ── 3. Download JARs ─────────────────────────────────────────────────
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

        // ── 4. JVM memory patch ──────────────────────────────────────────────
        PatchJvm(serverDir);

        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine(" Patch instalado com sucesso!");
        Console.WriteLine(" Reinicie o servidor XMage para aplicar.");
        Console.WriteLine("============================================");
        Pause();
        return 0;
    }

    // ── Detection ─────────────────────────────────────────────────────────────

    static string FindXMageServer()
    {
        // Registry: HKCU\Software\XMage\InstallDir
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

        // Common base folders
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

    /// Returns the mage-server path if found under basePath, else null.
    static string TryServerDir(string basePath)
    {
        if (string.IsNullOrEmpty(basePath)) return null;

        // basePath IS mage-server
        if (Directory.Exists(Path.Combine(basePath, "lib"))) return basePath;

        // basePath\mage-server
        string s1 = Path.Combine(basePath, "mage-server");
        if (Directory.Exists(Path.Combine(s1, "lib"))) return s1;

        // basePath\xmage\mage-server
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

    /// Detects the exact versioned JAR filename already present in dir.
    static string DetectJar(string dir, string pattern, string fallback)
    {
        if (!Directory.Exists(dir)) return fallback;

        string[] matches = Directory.GetFiles(dir, pattern);
        foreach (string path in matches)
        {
            string name = Path.GetFileName(path);
            // Exclude backups
            if (!name.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return fallback;
    }

    // ── Download ──────────────────────────────────────────────────────────────

    static bool DownloadJar(string jarName, string destPath, int step, int total)
    {
        Console.Write("[" + step + "/" + total + "] Baixando " + jarName + "... ");

        string backupPath = destPath + ".backup";
        try
        {
            // Backup existing file
            if (File.Exists(destPath))
                File.Copy(destPath, backupPath, overwrite: true);

            // WebClient follows HTTP redirects automatically (unlike Invoke-WebRequest issues)
            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "XMageAIPatch/1.0";
                wc.DownloadFile(BASE_URL + "/" + jarName, destPath);
            }

            // Validate
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

    // ── JVM patch ─────────────────────────────────────────────────────────────

    static void PatchJvm(string serverDir)
    {
        Console.Write("[4/4] Aplicando patch de memoria JVM (-Xmx4096m + G1GC)... ");

        string batPath = FindStartServerBat(serverDir);
        if (batPath == null) { Console.WriteLine("startServer.bat nao encontrado — ajuste manualmente."); return; }

        string content = File.ReadAllText(batPath);

        if (content.Contains("-Xmx4096m") && content.Contains("UseG1GC"))
        { Console.WriteLine("OK (ja configurado)"); return; }

        content = Regex.Replace(content, @"-Xmx\S+", "-Xmx4096m");
        if (!content.Contains("UseG1GC"))
            content = content.Replace("java ", "java -XX:+UseG1GC ");

        File.WriteAllText(batPath, content);
        Console.WriteLine("OK (atualizado)");
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
            if (File.Exists(c)) return Path.GetFullPath(c);
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
