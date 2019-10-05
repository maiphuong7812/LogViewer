using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("End write log!");
            Stopwatch stopWatch = new Stopwatch();
            BackgroundWorker bw = new BackgroundWorker();
            bool isCreate = (bool)createLogRadio.IsChecked;
            bool isMerge = (bool)mergeLogRadio.IsChecked;
            bw.DoWork += (send, ee) =>
            {
                stopWatch.Start();
                if (isCreate == true)
                {
                    int logNum = 0;
                    int logCount = 1500000;
                    List<StreamWriter> sWriters = new List<StreamWriter>();
                    for (int i = 0; i < 15; i++)
                    {
                        string path = AppDomain.CurrentDomain.BaseDirectory + "\\..\\..\\data\\Tmp" + i.ToString().PadLeft(2, '0') + ".log";
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        BufferedStream bs = new BufferedStream(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), 256 * 1024);
                        sWriters.Add(new StreamWriter(bs));
                    }

                    {
                        while (logNum < logCount)
                        {
                            foreach (var sWriter in sWriters)
                            {
                                var logLine = DateTime.Now.ToString("hh:mm:ss.fff") + " " + logNum.ToString().PadLeft(50, '0');
                                sWriter.WriteLine(logLine);
                            }
                            logNum++;
                        }

                        while (logNum < logCount + 50000)
                        {

                            var logLine = DateTime.Now.ToString("hh:mm:ss.fff") + " " + logNum.ToString().PadLeft(50, '0');
                            sWriters[11].WriteLine(logLine);
                            logNum++;
                        }
                    }

                    foreach (var stream in sWriters)
                    {
                        stream.Dispose();
                    }
                }
                else if (isMerge)
                {
                    string mergedPath = AppDomain.CurrentDomain.BaseDirectory + "\\..\\..\\data\\merge.log";
                    if (File.Exists(mergedPath))
                    {
                        File.Delete(mergedPath);
                    }
                    // Get all files in folder
                    string folder = AppDomain.CurrentDomain.BaseDirectory + "\\..\\..\\data";
                    var files = Directory.GetFiles(folder).ToList();
                    if (files == null)
                    {
                        return;
                    }
                    List<StreamReader> sReaders = new List<StreamReader>();
                    foreach (var file in files)
                    {
                        FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        BufferedStream bs = new BufferedStream(fs);
                        sReaders.Add(new StreamReader(bs));
                    }

                    //Read first line of each file to list
                    List<string> logLines = new List<string>();
                    for (int i = sReaders.Count - 1; i >= 0; i--)
                    {
                        var line = sReaders[i].ReadLine();
                        if (line != null)
                        {
                            logLines.Add(System.IO.Path.GetFileNameWithoutExtension(files[i]) + " " + line);
                        }
                        else
                        {
                            sReaders.RemoveAt(i);
                        }
                    }

                    // Loop to find earliest log then write to tmp file        
                    using (BufferedStream bs = new BufferedStream(new FileStream(mergedPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), 256 * 1024))
                    using (StreamWriter sWriter = new StreamWriter(bs))
                    {
                        while (logLines.Count > 0)
                        {
                            // Find earliest line
                            int index = FindEarliestLog(logLines);
                            string logLine = logLines[index];
                            logLines.RemoveAt(index);

                            // Write to file
                            sWriter.WriteLine(logLine);

                            // Add log line of file has earliest log line to list
                            var newLog = sReaders[sReaders.Count - index - 1].ReadLine();
                            if (newLog != null)
                            {
                                logLines.Insert(index, System.IO.Path.GetFileNameWithoutExtension(files[files.Count - index - 1]) + " " + newLog);
                            }
                            else
                            {
                                sReaders[files.Count - index - 1].Dispose();
                                sReaders.RemoveAt(files.Count - index - 1);
                                files.RemoveAt(files.Count - index - 1);
                            }
                        }
                    }
                    foreach (var reader in sReaders)
                    {
                        reader.Dispose();
                    }
                }
                else
                {
                    string mergedPath = AppDomain.CurrentDomain.BaseDirectory + "\\..\\..\\data\\merge.log";
                    if (File.Exists(mergedPath))
                    {
                        File.Delete(mergedPath);
                    }
                    // Get all files in folder
                    string folder = AppDomain.CurrentDomain.BaseDirectory + "\\..\\..\\data";
                    var files = Directory.GetFiles(folder).ToList();
                    if (files == null)
                    {
                        return;
                    }
                    //List<StreamReader> sReaders = new List<StreamReader>();
                    List<string> logLines = new List<string>();
                    foreach (var file in files)
                    {
                        //FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        //BufferedStream bs = new BufferedStream(fs);
                        //sReaders.Add(new StreamReader(bs));
                        string[] fileContents = File.ReadAllLines(file);
                        logLines.AddRange(fileContents);
                    }

                    logLines.Sort();

                    using (BufferedStream bs = new BufferedStream(new FileStream(mergedPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), 256 * 1024))
                    using (StreamWriter sWriter = new StreamWriter(bs))
                    {
                        foreach (var line in logLines)
                        {
                            //string newLine = "";
                            sWriter.WriteLine(line);
                        }
                    }
                }
                stopWatch.Stop();
            };

            bw.RunWorkerCompleted += (send, ee) =>
            {
                Console.WriteLine(stopWatch.ElapsedMilliseconds.ToString());
                MessageBox.Show("End merge file");
            };

            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Find earliest printed log in list
        /// </summary>
        /// <param name="logLst">List of first log line</param>
        /// <returns>index of earliest log. Return -1 if log list is invalid</returns>
        private static int FindEarliestLog(List<string> logLst)
        {
            if (logLst == null || logLst.Count == 0)
            {
                return -1;
            }

            // Get log time of each log line
            List<string> logTimes = new List<string>();
            foreach (var logLine in logLst)
            {
                logTimes.Add(GetLogTime(logLine));
            }

            // Get min log time index
            string min = logTimes.Min();
            int idx = logTimes.IndexOf(min);
            //string min = logTimes[0];
            //int idx = 0;
            //for (int i = 1; i < logTimes.Count; i++)
            //{
            //    if (String.Compare(logTimes[i], min) <= 0)
            //    {
            //        idx = i;
            //    }
            //}


            return idx;
        }

        private static string GetLogTime(string log)
        {
            if (String.IsNullOrEmpty(log))
            {
                return String.Empty;
            }
            else
            {
                var splittedLog = log.Split(' ');
                if (splittedLog.Length > 1)
                {
                    return splittedLog[1];
                }
                else
                {
                    return String.Empty;
                }
            }
        }
    }
}
