using UnityEngine;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace ModularProject.Runtime
{
	public static class FileUtils
	{
        public static bool TryReadFileToBytes (string fileFullName, out byte[] bytes)
        {
	        // todo gc optimize
            bytes = null;
            var fi = new FileInfo (fileFullName);
            if (!fi.Exists) {
                return false;
            }

            using FileStream fs = new FileStream (fileFullName, FileMode.Open, FileAccess.Read);
            bytes = new byte[fs.Length];
            fs.Read(bytes, 0, (int)fs.Length);
            fs.Close();
            return true;
        }
		public static byte[] ReadFileToBytes (string fileFullName)
		{
			// todo gc optimize
			var fi = new FileInfo(fileFullName);
			if (!fi.Exists)
			{
				Debug.LogError($"File [ {fileFullName} ] doesn't exist.");
				return null;
			}

			using FileStream fs = new FileStream (fileFullName, FileMode.Open, FileAccess.Read);
			var buffer = new byte[fs.Length];
			fs.Read(buffer, 0, (int)fs.Length);
			fs.Close();
			return buffer;
		}

        public static bool TryReadFileToString (string fileFullName, out string content)
        {
            content = string.Empty;
            if (!TryReadFileToBytes(fileFullName, out var bytes)) return false;
            if (null == bytes || bytes.Length == 0)
            {
	            return false;
            }
            content = Encoding.UTF8.GetString(bytes);
            return true;
        }

		public static string ReadFileToString (string fileFullName)
		{
			var bytes = ReadFileToBytes(fileFullName);
			if (null == bytes || bytes.Length == 0)
			{
				return null;
			}
			return Encoding.UTF8.GetString(bytes);
		}

		public static bool WriteBytesToFile (byte[] bytes, string fileFullName, bool overwrite = true)
		{
			// todo gc optimize
			var fi = new FileInfo(fileFullName);
			if (fi.Exists && !overwrite)
			{
				Debug.LogError($"File [ {fileFullName} ] already exist, write file failed.");
				return false;
			}

			using FileStream fs = new FileStream (fileFullName, FileMode.Create, FileAccess.Write);
			fs.Write(bytes, 0, (int)bytes.Length);
			fs.Flush();
			fs.Close();
			return true;
		}

		public static bool WriteStringToFile(string content, string fileFullName, bool overwrite = true)
		{
			if (string.IsNullOrEmpty(content))
			{
				Debug.LogError($"WriteStringToFile failed content IsNullOrEmpty! FileFullName: [ {fileFullName} ].");
				return false;
			}
			var bytes = Encoding.UTF8.GetBytes(content);
			return bytes.Length != 0 && WriteBytesToFile(bytes, fileFullName, overwrite);
		}

		public static void RenameFile (string fileFullName, string newFileFullName, bool overwrite = true)
		{
			if (!File.Exists(fileFullName))
			{
				Debug.LogError($"Source file [ {fileFullName} ] does not exist, rename file failed.");
				return;
			}
			if (File.Exists(newFileFullName))
			{
				if (overwrite)
				{
					File.Delete(newFileFullName);
				}
				else
				{
					Debug.LogError($"File [ {newFileFullName}]  already exist, rename file failed.");
					return;
				}
			}

			File.Move(fileFullName, newFileFullName);
		}

		public static void CopyFile(string sourceFileFullName, string destFileFullName, bool overwrite = true)
		{
			if (!File.Exists(sourceFileFullName))
			{
				Debug.LogError($"Source file [ {sourceFileFullName}]  does not exist, copy file failed.");
				return;
			}
			if (File.Exists(destFileFullName))
			{
				if (overwrite)
				{
					File.Delete(destFileFullName);
				}
				else
				{
					Debug.LogError($"File [ {destFileFullName} ] already exist, copy file failed.");
					return;
				}
			}
			File.Copy(sourceFileFullName, destFileFullName);
		}
		
		public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true, bool recursive = true)
		{
			// todo gc optimize
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath, overwrite);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (!recursive) return;
			foreach (DirectoryInfo subDir in dirs)
			{
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
				CopyDirectory(subDir.FullName, newDestinationDir, true);
			}
		}

		public static long GetFileHash (string fileFullName, out string hash)
		{
			using var fs = new FileStream(fileFullName, FileMode.Open);
			return GetStreamHash(fs, out hash);
		}

        public static string GetBytesHash (byte[] bytes)
        {
	        using var ms = new MemoryStream(bytes);
	        GetStreamHash(ms, out var md5);
	        return md5;
        }

        private static long GetStreamHash(Stream stream, out string hash)
        {
	        var sha256 = SHA256.Create();
	        var hashValue = sha256.ComputeHash(stream);
	        var size = stream.Length;
	        var sb = new StringBuilder();
	        var index = 0;
	        for (; index < hashValue.Length; index++)
	        {
		        var t = hashValue[index];
		        sb.Append(t.ToString("x2"));
	        }
	        hash = sb.ToString();
	        return size;
        }

		public static bool CheckDirectory (string path, bool createIfNotExist = false)
		{
			var exists = Directory.Exists(path);
			if (exists) return true;
			Directory.CreateDirectory(path);
			return false;
		}

        public static bool DeleteFolder (string path, bool recursive = true)
        {
	        // todo gc optimize
            var di = new DirectoryInfo (path);
            if (!di.Exists) return false;
            if ((di.GetFiles().Length != 0 || di.GetDirectories ().Length != 0) && !recursive)
	            return false;
            di.Delete (true);
            return false;
        }

        /// <summary>
        /// Clear folder,
        /// </summary>
        /// <param name="path"></param>
        /// <param name="safeDelete"></param>
        /// <returns></returns>
        public static bool ClearFolder(string path)
        {
	        // todo gc optimize
	        var di = new DirectoryInfo (path);
	        if (!di.Exists) return false;
	        foreach (var file in di.GetFiles())
	        {
		        file.Delete(); 
	        }
	        foreach (var dir in di.GetDirectories())
	        {
		        dir.Delete(true); 
	        }
			return true;
        }

        public static bool DeleteFile(string fullFileName)
        {
	        // todo gc optimize
            var fi = new FileInfo (fullFileName);
            if (!fi.Exists) return false;
            fi.Delete ();
            return true;
        }
	}
}