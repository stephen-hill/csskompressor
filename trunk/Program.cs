/*
 * Created by SharpDevelop.
 * User: shill2
 * Date: 11/05/2010
 * Time: 15:58
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CSS_Kompressor
{
	class Program
	{
		public static string CurrentDirectory { get; set; }
		public static string Filter { get; set; }
		public static string Output { get; set; }
		public static ConsoleKeyInfo Key { get; set; }
		public static Dictionary<string, string> Files { get; set; }
		public static Dictionary<string, DateTime> Modified { get; set; }
		public static FileSystemWatcher Watch { get; set; }
		
		public static void Main(string[] args)
		{
			WatchFiles();
		}
		
		public static void WatchFiles() {
			// Set variables
			CurrentDirectory = System.Environment.CurrentDirectory;
			Filter = "*.css";
			Output = "c.css";
			Key = new ConsoleKeyInfo();
			Files = new Dictionary<string, string>();
			Modified = new Dictionary<string, DateTime>();
			
			// Get List of Files
			var Dir = new DirectoryInfo(CurrentDirectory);
			var DirFiles = Dir.GetFiles(Filter);
			Array.Sort(DirFiles, delegate(FileInfo f1, FileInfo f2) {
				return f1.Name.CompareTo(f2.Name);
			});
			foreach (var File in DirFiles) {
				if (File.Name != Output) {
					Files.Add(File.Name, Kompress(File.FullName));
					Modified.Add(File.Name, new FileInfo(File.FullName).LastWriteTime);
				}
			}
			
			SaveToFile();
			
			// Set File System Watcher
			Watch = new FileSystemWatcher();
			Watch.Path = CurrentDirectory;
			Watch.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
			Watch.Filter = Filter;
			Watch.InternalBufferSize = 32768;
			
			// Set Event Listener
			Watch.Changed += new FileSystemEventHandler(OnChange);
            Watch.Created += new FileSystemEventHandler(OnCreate);
            Watch.Deleted += new FileSystemEventHandler(OnDelete);
            Watch.Renamed += new RenamedEventHandler(OnRename);
            Watch.Error += new ErrorEventHandler(WatcherError);
            
            // Start Event Listener
            Watch.EnableRaisingEvents = true;
            
            while (ConsoleKey.Escape != Key.Key) {
            	Thread.Sleep(250);
            	Key = Console.ReadKey(true);
            }
		}
		
		private static void WatcherError(object source, ErrorEventArgs e) {
			Exception watchException = e.GetException();
			Console.WriteLine("A FileSystemWatcher error has occurred: "
			             + watchException.Message);
			// We need to create new version of the object because the
			// old one is now corrupted
			Watch = new FileSystemWatcher();
			while (!Watch.EnableRaisingEvents)
			{
			try
			{
			   // This will throw an error at the
			   // watcher.NotifyFilter line if it can't get the path.
			   WatchFiles();
			   Console.WriteLine("I'm Back!!");
			}
			catch
			{
			   // Sleep for a bit; otherwise, it takes a bit of
			   // processor time
			   System.Threading.Thread.Sleep(5000);
			}
			}
		}
		
		private static void OnChange(object source, FileSystemEventArgs e) {
			if (e.Name != Output && Modified[e.Name] != new FileInfo(e.Name).LastWriteTime) {
				Files[e.Name] = Kompress(e.Name);
				Modified[e.Name] = new FileInfo(e.Name).LastWriteTime;
				Console.WriteLine("Updated " + e.Name);
				SaveToFile();
			}
		}
		
		private static void OnCreate(object source, FileSystemEventArgs e) {
			if (e.Name != Output) {
				Files.Add(e.Name, Kompress(e.Name));
				Modified.Add(e.Name, new FileInfo(e.Name).LastWriteTime);
				Console.WriteLine("Added " + e.Name);
				SaveToFile();
			}
		}
		
		private static void OnDelete(object source, FileSystemEventArgs e) {
			if (e.Name != Output) {
				Files.Remove(e.Name);
				Modified.Remove(e.Name);
				Console.WriteLine("Removed " + e.Name);
				SaveToFile();
			}
		}
		
		private static void OnRename(object source, RenamedEventArgs e) {
			if (e.Name != Output && e.OldName != Output) {
				Files.Remove(e.OldName);
				Modified.Remove(e.OldName);
				
				Files.Add(e.Name, Kompress(e.Name));
				Modified.Add(e.Name, new FileInfo(e.Name).LastWriteTime);
				
				Console.WriteLine("Renamed " + e.OldName + " to " + e.Name);
				SaveToFile();
			}
		}
		
		private static void SaveToFile() {
			var Writer = new StreamWriter(Output);
			foreach(var File in Files) {
				Writer.Write(File.Value);
			}
			Writer.Close();
			Writer.Dispose();
			//Console.WriteLine("Saved " + Output);
		}
		
		private static string Kompress(string Filename) {
			// Wait for file to become available
			while (IsFileLocked(new FileInfo(Filename)) == true) {
				Thread.Sleep(250);
				Console.WriteLine("Waiting for file to become available " + Filename);
			}
			
			// Load the CSS File
			var Reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read));
			var CSS = Reader.ReadToEnd();
			Reader.Close();
			Reader.Dispose();
			
            // Convert Standard to Browser Specific CSS Properties
            var matches = Regex.Matches(CSS, @"border-radius:([\d\w\s]+)[;}]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match match in matches) {
            	//CSS = CSS.Insert(match.Index, "-moz-border-radius:" + match.Captures[0] + ";");
            	foreach (Capture element in match.Captures) {
            		Console.WriteLine(element.Value);
            	}
            }
			
			for (int i = 0; i < 2; i++) {
				// Remove New Lines and Tabs
				CSS = CSS.Replace("\n", String.Empty);
				CSS = CSS.Replace("\r", String.Empty);
				CSS = CSS.Replace("\f", String.Empty);
				CSS = CSS.Replace("\t", String.Empty);
				
				// Remove Specific Rules
                CSS = CSS.Replace(";}", "}");
                CSS = CSS.Replace(": ", ":");
                CSS = CSS.Replace(", ", ",");
                CSS = CSS.Replace(" }", "}");
                CSS = CSS.Replace("} ", "}");
                CSS = CSS.Replace(" } ", "}");
                CSS = CSS.Replace(" {", "{");
                CSS = CSS.Replace("{ ", "{");
                CSS = CSS.Replace(" { ", "{");
                
                // Colours
                CSS = CSS.Replace("#ff0000", "red");
                CSS = CSS.Replace("#f00", "red");
                CSS = CSS.Replace("rgb(255,0,0)", "red");

                CSS = CSS.Replace("#D2B48C", "tan");
                CSS = CSS.Replace("rgb(210,180,140)", "tan");
                
                CSS = CSS.Replace("rgb(0,0,0)", "#000");
                CSS = CSS.Replace("rgb(0,255,0)", "#0f0");
                CSS = CSS.Replace("rgb(0,0,255)", "#00f");
                CSS = CSS.Replace("rgb(255,255,0)", "#ff0");
                CSS = CSS.Replace("rgb(0,255,255)", "#0ff");
                CSS = CSS.Replace("rgb(255,0,255)", "#f0f");
                CSS = CSS.Replace("rgb(255,255,255)", "#fff");
                
                // Remove Comments
                CSS = Regex.Replace(CSS, @"/\*(.|[\r\n])*?\*/", String.Empty, RegexOptions.IgnoreCase);
			}
			return CSS;
		}
		
	public static bool IsFileLocked(FileInfo file)
    {
        FileStream stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }

        //file is not locked
        return false;
    }
	}
}