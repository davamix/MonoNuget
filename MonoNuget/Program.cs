using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoNuget
{
	class Program
	{
		private static string _nugetPath;
		private static string _packageName;
		private static string _projectFolder;
		private static string _packagesDirectory;
		private static string _libsDirectory;
		private static List<string> _packagesDownloaded;

		static void Main(string[] args)
		{
			_packagesDownloaded = new List<string>();

			if (!ExtractParams(args))
			{
				ShowHelp();
				return;
			}

			if (!CheckNugetPath())
			{
				Console.WriteLine("NuGet.exe doesn't exists in the path {0}", _nugetPath);
				return;
			}
				

			ConfigureLibsDirectory();
			ConfigurePackagesDirectory();

			DownloadNugetPackage();

			CopyLibraryToLibsDirectory();

			Console.WriteLine("\nProcess finished. Press any key to continue...");
			Console.Read();
		}

		private static bool CopyLibraryToLibsDirectory()
		{
			Console.WriteLine("\nCopying libraries from Packages to Libs directory");
			foreach (var package in _packagesDownloaded)
			{
				Console.WriteLine("... {0}", package);

				var libDirectory = Path.Combine(_packagesDirectory, package);
				if (!Directory.Exists(libDirectory))
					return false;

				var libPath = GetLibraryFromPackageDirectory(libDirectory);

				if (string.IsNullOrEmpty(libPath))
					return false;

				MoveLibraryToLibsDirectory(libPath);

			}
			return true;
		}

		private static string GetLibraryFromPackageDirectory(string libDirectory)
		{
			var files = Directory.GetFiles(libDirectory, "*.dll", SearchOption.AllDirectories);
			if (files.Length > 0)
				return files[0];

			return string.Empty;
		}

		private static void MoveLibraryToLibsDirectory(string libPath)
		{
			var fileName = new FileInfo(libPath).Name;
			var destinationPath = Path.Combine(_libsDirectory, fileName);

			File.Copy(libPath, destinationPath);
		}

		private static void DownloadNugetPackage()
		{
			Console.WriteLine("\nStarting NuGet...");
			var args = string.Format("install {0} -OutputDirectory {1}", _packageName, _packagesDirectory);


			var p = new Process();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.FileName = _nugetPath;
			p.StartInfo.Arguments = args;
			p.OutputDataReceived += ExtractPackageName;
			p.Start();
			
			p.BeginOutputReadLine();
			p.WaitForExit();
		}

		private static void ExtractPackageName(object sender, DataReceivedEventArgs args)
		{
			if (string.IsNullOrEmpty(args.Data))
				return;

			Console.WriteLine(args.Data);

			if (!args.Data.Contains("Successfully"))
				return;

			var p = args.Data.Split('\'');

			var packageName = p[1].Replace(" ", ".");

			if (!_packagesDownloaded.Contains(packageName))
				_packagesDownloaded.Add(packageName);

		}

		private static void ConfigurePackagesDirectory()
		{
			_packagesDirectory = "Packages";
			if (!string.IsNullOrEmpty(_projectFolder))
				_packagesDirectory = Path.Combine(_projectFolder, _packagesDirectory);

			if (!Directory.Exists(_packagesDirectory))
			{
				Console.WriteLine("\nCreating Packages directory in {0}", _packagesDirectory);
				Directory.CreateDirectory(_packagesDirectory);
			}
				
		}

		private static void ConfigureLibsDirectory()
		{
			_libsDirectory = "Libs";
			if (!string.IsNullOrEmpty(_projectFolder))
				_libsDirectory = Path.Combine(_projectFolder, _libsDirectory);

			if (!Directory.Exists(_libsDirectory))
			{
				Console.WriteLine("\nCreating Libs directory in {0}", _libsDirectory);
				Directory.CreateDirectory(_libsDirectory);
			}
				
		}

		private static bool CheckNugetPath()
		{
			return File.Exists(_nugetPath);
		}

		private static void ShowHelp()
		{
			Console.WriteLine("Usage: MonoNuget.exe -nuget nugetPath [-project projectPath] packageName");
			Console.WriteLine("-nuget\t\tPath of NuGet.exe executable");
			Console.WriteLine("-project\t\tPath of project where package and libs will be download");
			Console.WriteLine("packageName\t\tName of package to download");
		}

		private static bool ExtractParams(string[] args)
		{
			if (args.Contains("-nuget"))
				_nugetPath = args[Array.IndexOf(args, "-nuget") + 1];
			else
				return false;

			if (args.Contains("-project"))
				_projectFolder = args[Array.IndexOf(args, "-project") + 1];

			_packageName = args[args.Length - 1];

			return true;
		}
	}
}
