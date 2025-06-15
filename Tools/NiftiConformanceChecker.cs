using System;
using System.IO;
using System.Linq;
using Nifti.NET;

namespace Nifti.NET.Tools
{
    /// <summary>
    /// Command-line tool for checking NIfTI file conformance to the NIfTI-1 specification
    /// Usage: NiftiConformanceChecker.exe [options] <file1> [file2] [...]
    /// </summary>
    public class NiftiConformanceChecker
    {
        private static bool verbose = false;
        private static bool showWarnings = true;
        private static bool showInfo = false;
        private static bool recursive = false;

        public static void Main(string[] args)
        {
            Console.WriteLine("NIfTI-1 Conformance Checker");
            Console.WriteLine("===========================");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var filesToCheck = ParseArguments(args);

            if (filesToCheck.Length == 0)
            {
                Console.WriteLine("Error: No files specified for checking.");
                ShowUsage();
                return;
            }

            int totalFiles = 0;
            int validFiles = 0;
            int errorFiles = 0;
            int warningFiles = 0;

            foreach (var filePattern in filesToCheck)
            {
                var files = GetFilesToCheck(filePattern);
                
                foreach (var file in files)
                {
                    totalFiles++;
                    Console.WriteLine($"Checking: {file}");
                    
                    var result = NiftiValidator.ValidateFile(file);
                    
                    if (result.IsValid)
                    {
                        validFiles++;
                        Console.WriteLine("  ✓ VALID");
                        
                        if (result.HasWarnings && showWarnings)
                        {
                            warningFiles++;
                            Console.WriteLine("  ⚠ Warnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Console.WriteLine($"    - {warning}");
                            }
                        }
                        
                        if (showInfo && result.Info.Count > 0)
                        {
                            Console.WriteLine("  ℹ Info:");
                            foreach (var info in result.Info)
                            {
                                Console.WriteLine($"    - {info}");
                            }
                        }
                    }
                    else
                    {
                        errorFiles++;
                        Console.WriteLine("  ✗ INVALID");
                        Console.WriteLine("  ✗ Errors:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"    - {error}");
                        }
                        
                        if (result.HasWarnings && showWarnings)
                        {
                            Console.WriteLine("  ⚠ Warnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Console.WriteLine($"    - {warning}");
                            }
                        }
                    }
                    
                    if (verbose)
                    {
                        Console.WriteLine("  Full validation report:");
                        Console.WriteLine(result.ToString().Replace("\n", "\n    "));
                    }
                    
                    Console.WriteLine();
                }
            }

            // Summary
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Total files checked: {totalFiles}");
            Console.WriteLine($"  Valid files: {validFiles}");
            Console.WriteLine($"  Files with errors: {errorFiles}");
            Console.WriteLine($"  Files with warnings: {warningFiles}");
            
            var successRate = totalFiles > 0 ? (double)validFiles / totalFiles * 100 : 0;
            Console.WriteLine($"  Success rate: {successRate:F1}%");

            // Exit with error code if any files failed validation
            if (errorFiles > 0)
            {
                Environment.Exit(1);
            }
        }

        private static string[] ParseArguments(string[] args)
        {
            var files = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    
                    case "-q":
                    case "--quiet":
                        showWarnings = false;
                        showInfo = false;
                        break;
                    
                    case "-w":
                    case "--warnings":
                        showWarnings = true;
                        break;
                    
                    case "--no-warnings":
                        showWarnings = false;
                        break;
                    
                    case "-i":
                    case "--info":
                        showInfo = true;
                        break;
                    
                    case "-r":
                    case "--recursive":
                        recursive = true;
                        break;
                    
                    case "-h":
                    case "--help":
                        ShowUsage();
                        Environment.Exit(0);
                        break;
                    
                    default:
                        if (!args[i].StartsWith("-"))
                        {
                            files.Add(args[i]);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unknown option '{args[i]}'");
                        }
                        break;
                }
            }
            
            return files.ToArray();
        }

        private static string[] GetFilesToCheck(string pattern)
        {
            var files = new System.Collections.Generic.List<string>();
            
            if (File.Exists(pattern))
            {
                files.Add(pattern);
            }
            else if (Directory.Exists(pattern))
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var niftiExtensions = new[] { "*.nii", "*.hdr", "*.img", "*.nii.gz", "*.hdr.gz", "*.img.gz" };
                
                foreach (var extension in niftiExtensions)
                {
                    files.AddRange(Directory.GetFiles(pattern, extension, searchOption));
                }
            }
            else
            {
                // Try as a file pattern
                try
                {
                    var directory = Path.GetDirectoryName(pattern) ?? ".";
                    var fileName = Path.GetFileName(pattern);
                    
                    if (Directory.Exists(directory))
                    {
                        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        files.AddRange(Directory.GetFiles(directory, fileName, searchOption));
                    }
                }
                catch
                {
                    Console.WriteLine($"Warning: Could not find files matching pattern '{pattern}'");
                }
            }
            
            return files.Distinct().OrderBy(f => f).ToArray();
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: NiftiConformanceChecker [options] <file1> [file2] [directory] [pattern]...");
            Console.WriteLine();
            Console.WriteLine("Validates NIfTI files against the NIfTI-1 specification.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help        Show this help message");
            Console.WriteLine("  -v, --verbose     Show detailed validation information");
            Console.WriteLine("  -q, --quiet       Suppress warnings and info messages");
            Console.WriteLine("  -w, --warnings    Show warnings (default: on)");
            Console.WriteLine("  --no-warnings     Hide warnings");
            Console.WriteLine("  -i, --info        Show informational messages");
            Console.WriteLine("  -r, --recursive   Search directories recursively");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  NiftiConformanceChecker brain.nii");
            Console.WriteLine("  NiftiConformanceChecker *.nii");
            Console.WriteLine("  NiftiConformanceChecker -r /data/nifti/");
            Console.WriteLine("  NiftiConformanceChecker -v --info brain.nii fmri.nii.gz");
            Console.WriteLine();
            Console.WriteLine("Exit codes:");
            Console.WriteLine("  0  All files are valid");
            Console.WriteLine("  1  One or more files have validation errors");
        }
    }
}