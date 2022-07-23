#if UNITY_EDITOR

namespace QuickEditor.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public static class QuickEditorFileStaticAPI
    {
        #region Directory

        public static bool IsDir(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static string Normalize(this string path)
        {
            return Path.GetFullPath(path);
        }

        public static bool EnsureDirectory(string path)
        {
            if (path.Equals(string.Empty)) { return false; }
            try
            {
                path = Path.GetDirectoryName(path);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AssetDatabase.Refresh();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception while create directory at '{0}': {1}", path, e);
                return false;
            }
        }

        public static bool MakeDir(string path)
        {
            path = Normalize(path);
            return EnsureDirectory(path);
        }

        public static bool DeleteDirectory(string path, bool recursive)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception while delete directory at '{0}': {1}", path, e);
                return false;
            }
        }

        #endregion Directory

        public static bool Exists(string path)
        {
            if (path.Equals(string.Empty)) { return false; }
            path = Normalize(path);
            return File.Exists(path) || Directory.Exists(path);
        }

        #region CreateFile

        public static bool CreateFile(string path, string name)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) { return false; }
            return CreateFile(Path.Combine(path, name));
        }

        public static bool CreateFile(string path)
        {
            EnsureDirectory(path);
            try
            {
                if (!Exists(path))
                {
                    var fs = File.Create(path);
                    fs.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception while Create File at '{0}': {1}", path, e);
                return false;
            }
        }

        #endregion CreateFile

        #region DeleteFile

        public static bool DeleteFile(string path, string name)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) { return false; }
            if (File.Exists(path))
            {
                return DeleteFile(Path.Combine(path, name));
            }
            return false;
        }

        public static bool DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    AssetDatabase.Refresh();
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }

        #endregion DeleteFile

        public static bool Copy(string sourcePath, string destPath, bool overwrite = true)
        {
            sourcePath = Normalize(sourcePath);
            destPath = Normalize(destPath);
            try
            {
                if (File.Exists(sourcePath))
                {
                    EnsureDirectory(destPath);

                    if (IsDir(sourcePath))
                    {
                        var files = Directory.GetFiles(sourcePath);
                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileName(file);
                            File.Copy(file, Path.Combine(destPath, fileName), overwrite);
                        }

                        foreach (var info in Directory.GetDirectories(sourcePath))
                        {
                            Copy(info, Path.Combine(destPath, Path.GetFileName(info)));
                        }
                    }
                    else
                    {
                        var fileName = Path.GetFileName(sourcePath);
                        File.Copy(sourcePath, Path.Combine(destPath, fileName), overwrite);
                    }
                    AssetDatabase.Refresh();
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            return false;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        public static bool WriteFile(string path, string data)
        {
            try
            {
                EnsureDirectory(path);
                StreamWriter sw = new StreamWriter(path);
                sw.Write(data);
                sw.Flush();
                sw.Close();
                sw.Dispose();
                sw = null;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            return false;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        public static bool WriteFile(string path, ref byte[] bytes)
        {
            try
            {
                EnsureDirectory(path);
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                fs.Close();
                fs.Dispose();
                fs = null;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            return false;
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        public static string ReadFile(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (info.Exists)
                {
                    FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    int len = (int)info.Length;
                    byte[] bytes = new byte[len];
                    fs.Read(bytes, 0, len);
                    string dataStr = System.Text.Encoding.UTF8.GetString(bytes);
                    fs.Close();
                    fs.Dispose();
                    fs = null;
                    return dataStr;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        public static void ReadFile(string path, ref List<string> list)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (info.Exists)
                {
                    string line;
                    StreamReader sr = new StreamReader(path);
                    while ((line = sr.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                    sr.Close();
                    sr.Dispose();
                    sr = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        public static byte[] ReadFileBytes(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (info.Exists)
                {
                    FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    int len = (int)info.Length;
                    byte[] bytes = new byte[len];
                    fs.Read(bytes, 0, len);
                    fs.Close();
                    fs.Dispose();
                    fs = null;
                    return bytes;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            return null;
        }

        public static long GetSize(string path)
        {
            path = Normalize(path ?? string.Empty);
            long size = 0;
            if (IsDir(path))
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    size += new FileInfo(file).Length;
                }

                foreach (var info in Directory.GetDirectories(path))
                {
                    size += GetSize(info);
                }
            }
            else
            {
                size += (new FileInfo(path)).Length;
            }

            return size;
        }
    }
}
#endif