using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

/// <summary>
/// XMage AI Patch Installer
/// - Auto-detects XMage installation via registry + common paths
/// - Detects versioned JAR filenames already on disk (any XMage server version)
/// - Downloads 3 player JARs from GitHub Releases
/// - Downloads GameChangerRegistry.class and injects into core mage-*.jar
///   (survives Grath updates that wipe custom mage.jar; re-run this patcher)
/// - Patches startServer.bat + installed.properties for JVM heap / G1GC / saveGameHistory
/// </summary>
class XMageInstaller
{
    const string BASE_URL = "https://github.com/dinga-hub/Xmage-improved/releases/latest/download";
    const long   MIN_JAR_BYTES = 10240; // 10 KB sanity check
    const long   MIN_CLASS_BYTES = 200;
    const string GC_CLASS_ASSET = "GameChangerRegistry.class";
    const string GC_ENTRY = "mage/cards/repository/GameChangerRegistry.class";

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

        // Exclude mage-player-ai-mcts / draftbot / mad — glob "mage-player-ai-*.jar" matches those too.
        // Grath 1.4.60+ renamed the MAD plugin: mage-player-ai-ma-* → mage-player-ai-mad-*.
        string jarAi    = DetectJar(libDir,     "mage-player-ai-*.jar",
            "mage-player-ai-mcts-", "mage-player-ai-draftbot-", "mage-player-ai-mad-", "mage-player-ai-ma-");
        string jarAiMa  = DetectMadPluginJar(pluginsDir);
        string jarHuman = DetectJar(pluginsDir, "mage-player-human-*.jar");
        string jarCore  = DetectCoreJar(libDir);

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
            Console.WriteLine("ERRO: nao encontrei mage-player-ai-mad-*.jar nem mage-player-ai-ma-*.jar em plugins\\.");
            Pause(); return 1;
        }
        if (jarHuman == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: nao encontrei mage-player-human-*.jar em plugins\\.");
            Pause(); return 1;
        }
        if (jarCore == null)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: nao encontrei mage-*.jar core em lib\\.");
            Pause(); return 1;
        }

        Console.WriteLine();
        Console.WriteLine("JARs detectados (independentes da versao do servidor):");
        Console.WriteLine("  lib\\"     + jarAi);
        Console.WriteLine("  plugins\\" + jarAiMa);
        Console.WriteLine("  plugins\\" + jarHuman);
        Console.WriteLine("  lib\\"     + jarCore + "  (core — recebe GameChangerRegistry)");
        Console.WriteLine();

        bool ok = true;
        ok = ok && DownloadJar("mage-player-ai.jar",    Path.Combine(libDir,     jarAi),    1, 5);
        ok = ok && DownloadJar("mage-player-ai-ma.jar", Path.Combine(pluginsDir, jarAiMa),  2, 5);
        ok = ok && DownloadJar("mage-player-human.jar", Path.Combine(pluginsDir, jarHuman), 3, 5);
        ok = ok && InjectGameChangerRegistry(Path.Combine(libDir, jarCore), 4, 5);

        if (!ok)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: instalacao falhou. Verifique sua conexao e tente novamente.");
            Pause(); return 1;
        }

        PatchJvmAndMemoryFiles(serverDir, 5, 5);

        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine(" Patch instalado com sucesso!");
        Console.WriteLine(" Reinicie o servidor XMage para aplicar.");
        Console.WriteLine(" Apos update oficial do Grath: rode este");
        Console.WriteLine(" instalador de novo (core + players).");
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
    static string DetectJar(string dir, string pattern, params string[] excludeNameContains)
    {
        if (!Directory.Exists(dir)) return null;

        string[] paths = Directory.GetFiles(dir, pattern);
        var candidates = paths
            .Where(p =>
            {
                string n = Path.GetFileName(p);
                if (n.EndsWith(".backup", StringComparison.OrdinalIgnoreCase)) return false;
                foreach (string ex in excludeNameContains)
                {
                    if (n.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0) return false;
                }
                return true;
            })
            .OrderByDescending(p => new FileInfo(p).LastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0) return null;
        return Path.GetFileName(candidates[0]);
    }

    /// <summary>
    /// Core framework JAR (mage-1.4.60.jar), not player/game/sets/server modules.
    /// Same exclusion list as build-and-deploy-ai.bat.
    /// </summary>
    static string DetectCoreJar(string libDir)
    {
        string[] exclude =
        {
            "mage-common-", "mage-sets-", "mage-server-", "mage-game-",
            "mage-player-", "mage-tournament-", "mage-ai-",
        };
        return DetectJar(libDir, "mage-*.jar", exclude);
    }

    /// <summary>
    /// MAD AI plugin: prefer current Grath name (ai-mad), fall back to older ai-ma.
    /// </summary>
    static string DetectMadPluginJar(string pluginsDir)
    {
        string mad = DetectJar(pluginsDir, "mage-player-ai-mad-*.jar");
        if (mad != null) return mad;
        return DetectJar(pluginsDir, "mage-player-ai-ma-*.jar");
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
                wc.Headers["User-Agent"] = "XMageAIPatch/1.1";
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

    /// <summary>
    /// Downloads GameChangerRegistry.class and upserts it into the core mage JAR (ZIP).
    /// WHY inject instead of replacing mage.jar: full Mage rebuild on Grath 1.4.60 can
    /// break cards (NoSuchMethodError). One class is ABI-safe and restores Sprint 19 GC list.
    /// </summary>
    static bool InjectGameChangerRegistry(string coreJarPath, int step, int total)
    {
        Console.Write("[" + step + "/" + total + "] GameChangerRegistry no core... ");

        string backupPath = coreJarPath + ".backup";
        string tempClass = Path.Combine(Path.GetTempPath(), "xmage-ai-" + GC_CLASS_ASSET);

        try
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "XMageAIPatch/1.1";
                wc.DownloadFile(BASE_URL + "/" + GC_CLASS_ASSET, tempClass);
            }

            long classSize = new FileInfo(tempClass).Length;
            if (classSize < MIN_CLASS_BYTES)
            {
                Console.WriteLine("ERRO: class muito pequena (" + classSize + " bytes).");
                return false;
            }

            if (File.Exists(coreJarPath))
                File.Copy(coreJarPath, backupPath, overwrite: true);

            using (FileStream fs = new FileStream(coreJarPath, FileMode.Open, FileAccess.ReadWrite))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                ZipArchiveEntry existing = zip.GetEntry(GC_ENTRY);
                if (existing != null)
                    existing.Delete();

                ZipArchiveEntry entry = zip.CreateEntry(GC_ENTRY, CompressionLevel.Optimal);
                using (Stream entryStream = entry.Open())
                using (FileStream classStream = File.OpenRead(tempClass))
                {
                    classStream.CopyTo(entryStream);
                }
            }

            Console.WriteLine("OK (injetado em " + Path.GetFileName(coreJarPath) + ")");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERRO: " + ex.Message);
            RestoreBackup(backupPath, coreJarPath);
            return false;
        }
        finally
        {
            try { if (File.Exists(tempClass)) File.Delete(tempClass); } catch { /* ignore */ }
        }
    }

    static void RestoreBackup(string backupPath, string destPath)
    {
        try { if (File.Exists(backupPath)) File.Copy(backupPath, destPath, overwrite: true); }
        catch { /* best-effort */ }
    }

    static void PatchJvmAndMemoryFiles(string serverDir, int step, int total)
    {
        Console.WriteLine("[" + step + "/" + total + "] Memoria JVM (-Xmx4096m + G1GC + saveGameHistory)");

        string batPath = FindStartServerBat(serverDir);
        if (batPath != null)
            PatchStartServerBat(batPath);
        else
            Console.WriteLine("  AVISO: startServer.bat nao encontrado.");

        string props = FindInstalledProperties(serverDir);
        if (props != null)
            PatchInstalledProperties(props);

        if (batPath == null && props == null)
            Console.WriteLine("  AVISO: nenhum arquivo de memoria encontrado; ajuste manualmente se precisar.");
    }

    static void PatchStartServerBat(string path)
    {
        string label = Path.GetFileName(path);
        string content = File.ReadAllText(path);

        if (content.Contains("-Xmx4096m") && content.Contains("UseG1GC") && content.Contains("saveGameHistory=true"))
        {
            Console.WriteLine();
            Console.WriteLine("  [OK] " + label);
            return;
        }

        content = Regex.Replace(content, @"-Xmx\S+", "-Xmx4096m");
        if (!content.Contains("UseG1GC"))
            content = content.Replace("java ", "java -XX:+UseG1GC ");
        if (!content.Contains("saveGameHistory=true"))
            content = content.Replace("java ", "java -Dxmage.dataCollectors.saveGameHistory=true ");

        File.WriteAllText(path, content);
        Console.WriteLine();
        Console.WriteLine("  [ATUALIZADO] " + path);
    }

    /// <summary>
    /// Launcher reads xmage.server.javaopts from installed.properties (parent of mage-server).
    /// </summary>
    static void PatchInstalledProperties(string path)
    {
        const string key = "xmage.server.javaopts=";
        string[] lines = File.ReadAllLines(path);
        bool changed = false;
        var output = new List<string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine;
            if (!line.StartsWith(key))
            {
                output.Add(line);
                continue;
            }

            string val = line.Substring(key.Length);
            if (!val.Contains("-Xmx4096m"))
            {
                val = Regex.Replace(val, @"-Xmx\S+", "-Xmx4096m");
                changed = true;
            }
            if (!val.Contains("UseG1GC"))
            {
                val += " -XX\\:+UseG1GC";
                changed = true;
            }
            if (!val.Contains("saveGameHistory"))
            {
                val += " -Dxmage.dataCollectors.saveGameHistory\\=true";
                changed = true;
            }

            output.Add(key + val);
        }

        if (!changed && File.ReadAllText(path).Contains("saveGameHistory"))
        {
            Console.WriteLine();
            Console.WriteLine("  [OK] installed.properties");
            return;
        }

        File.WriteAllLines(path, output);
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
