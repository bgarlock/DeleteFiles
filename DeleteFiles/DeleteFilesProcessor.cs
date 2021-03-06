﻿using DeleteFiles.Properties;
using System;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace DeleteFiles
{
    public class DeleteFilesProcessor
    {

        public bool ProcessFiles(DeleteFilesCommandLineParser parser)
        {
            if (!Directory.Exists(parser.Path))
            {
                OnShowMessage(Resources.StartFolderDoesnTExist + parser.Path);
                return false;
            }

            return ProcessFolder(parser.Path, parser);
        }

        protected bool ProcessFolder(string activeFolder, DeleteFilesCommandLineParser parser)
        {
            string[] files;

            try
            {
                files = Directory.GetFiles(activeFolder, parser.FileSpec);
            }
            catch (Exception e)
            {
                OnShowMessage(Resources.ErrorOpening + activeFolder + ". " + e.GetBaseException().Message);
                return false;
            }
            bool success = true;
            foreach (var file in files)
            {
                try
                {
                    if (IsFileToBeDeleted(file, parser))
                    {
                        if (!parser.DisplayOnly)
                        {
                            if (parser.UseRecycleBin)
                                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            else
                                File.Delete(file);
                        }
                        OnShowMessage(Resources.Deleting + file);
                    }
                }
                catch
                {
                    OnShowMessage(Resources.FailedToDelete + file);
                    success = false;
                }
            }

            if (parser.Recursive)
            {
                var dirs = Directory.GetDirectories(activeFolder);
                foreach (var dir in dirs)
                {
                    success = ProcessFolder(dir, parser);
                    if (success && parser.RemoveEmptyFolders)
                    {
                        if (!Directory.GetFiles(dir).Any() && !Directory.GetDirectories(dir).Any())
                            try
                            {
                                if (!parser.DisplayOnly)
                                {
                                    if (parser.UseRecycleBin)
                                        FileSystem.DeleteDirectory(dir, UIOption.OnlyErrorDialogs,
                                                                    RecycleOption.SendToRecycleBin);
                                    else
                                        Directory.Delete(dir);
                                }
                                OnShowMessage(Resources.DeletingDirectory + dir);
                            }
                            catch
                            {
                                OnShowMessage(Resources.FailedToDeleteDirectory + dir);
                            }
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Determines if file is to be deleted from disk
        /// 
        /// Checks:
        /// parser.Seconds from current time
        /// parser.Days from current time
        /// if neither is set file can be deleted
        /// </summary>
        /// <param name="file"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        private bool IsFileToBeDeleted(string file, DeleteFilesCommandLineParser parser)
        {
            if (parser.Seconds > -1)
            {
                var ftime = File.GetLastWriteTimeUtc(file);
                if (DateTime.UtcNow > ftime.AddSeconds(parser.Seconds))
                    return true;
                return false;
            }
            if (parser.Days > -1)
            {
                var ftime = File.GetLastWriteTime(file);
                if (DateTime.Now.Date >= ftime.Date.AddDays(parser.Days))
                    return true;
                return false;
            }

            // if neither days or seconds were provided delete all files
            return true;
        }

        /// <summary>
        /// Event that allows you to override the output that is sent 
        /// by this class. If not set output is sent to the Console.
        /// </summary>
        public event Action<string> ShowMessage;

        public virtual void OnShowMessage(string message)
        {
            if (ShowMessage != null)
                ShowMessage(message);
            else
                Console.WriteLine(message);
        }

    }

}
