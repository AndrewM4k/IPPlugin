using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Exception = System.Exception;

[assembly: CommandClass(typeof(AutoCAD.IPPlugin.Net8.Commands.ActionCommands))]

namespace AutoCAD.IPPlugin.Net8.Commands
{
    public static class ActionCommands
    {
        [CommandMethod("RUN_IP_CMD")]
        public static void RunIpCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var db = doc.Database;
            var ed = doc.Editor;

            try
            {
                //IP-адрес
                ed.WriteMessage("\nRetrieving IPv4 address...");
                string ip = GetIPv4Address();
                ed.WriteMessage($"\nYour IPv4: {ip}");

                // Создание и поворот MText
                CreateRotatedMText(db, ip, ed);
                ed.WriteMessage("\nText created in model space");

                // Загрузка DWG
                ed.WriteMessage("\nLoading DWG file...");
                LoadDwg(ed);

                // Логирование
                Log("Plugin execution finished!");
                ed.WriteMessage("\nOperation completed successfully!");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        private static void CreateRotatedMText(Database db, string ip, Editor ed)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    var btr = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);

                    // Центр чертежа
                    var viewCenter = new Point3d(0, 0, 0);

                    // MText
                    var mtext = new MText
                    {
                        Contents = $"Your public IPv4: {ip}",
                        Location = viewCenter,
                        TextHeight = 15,
                        Layer = "0",
                        ColorIndex = 1, // Красный
                        Attachment = AttachmentPoint.MiddleCenter
                    };

                    // Поворот на 90.5 градусов по часовой стрелке
                    mtext.TransformBy(Matrix3d.Rotation(
                        angle: 90.5 * Math.PI / 180.0,
                        axis: Vector3d.ZAxis,
                        center: mtext.Location
                    ));

                    // Добавление
                    btr.AppendEntity(mtext);
                    tr.AddNewlyCreatedDBObject(mtext, true);

                    tr.Commit();

                    ZoomToPoint(ed, viewCenter);
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    throw new Exception($"Text creation failed: {ex.Message}");
                }
            }
        }

        private static void ZoomToPoint(Editor ed, Point3d center)
        {
            using (var view = ed.GetCurrentView())
            {
                view.CenterPoint = new Point2d(center.X, center.Y);
                view.Height = 100;
                view.Width = 100;
                ed.SetCurrentView(view);
            }
        }

        private static void LoadDwg(Editor ed)
        {
            ProgressWindow progressWindow = null;
            Thread progressThread = null;
            AutoResetEvent windowReadyEvent = null;

            try
            {
                string heavyDwgPath = @"A:\solar-heating-system.dwg"; // Обновите путь

                if (!File.Exists(heavyDwgPath))
                {
                    ed.WriteMessage("\nHeavy DWG file not found. Creating a sample drawing...");
                    CreateSimpleDwg(); // Создаем простой чертеж
                    return;
                }

                windowReadyEvent = new AutoResetEvent(false);

                // Поток для окна прогресса
                progressThread = new Thread(() =>
                {
                    try
                    {
                        progressWindow = new ProgressWindow();
                        progressWindow.Closed += (sender, args) =>
                            progressWindow.Dispatcher.InvokeShutdown();

                        progressWindow.Show();
                        windowReadyEvent.Set();
                        Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Progress window error: " + ex.Message);
                    }
                });

                progressThread.SetApartmentState(ApartmentState.STA);
                progressThread.IsBackground = true;
                progressThread.Start();

                // Ожидание инициализации окна
                if (!windowReadyEvent.WaitOne(3000))
                {
                    ed.WriteMessage("\nProgress window did not open in time.");
                }

                // Основной файл, открытие
                Application.DocumentManager.Open(heavyDwgPath, false);
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError loading DWG: {ex.Message}");
            }
            finally
            {
                if (progressWindow != null)
                {
                    try
                    {
                        progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error closing progress window: " + ex.Message);
                    }
                }
                windowReadyEvent?.Dispose();
            }
        }

        private static void CreateSimpleDwg()
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "sample.dwg");
                var docName = Path.GetFileName(tempPath);

                foreach (Document doc in Application.DocumentManager)
                {
                    if (doc.Name.Equals(docName, StringComparison.OrdinalIgnoreCase))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;
                        return;
                    }
                }

                using (var db = new Database(true, true))
                {
                    db.SaveAs(tempPath, DwgVersion.Current);
                    Application.DocumentManager.Open(tempPath, false);
                }
            }
            catch (Exception ex)
            {
                //игнорируем
            }
        }

        private static string GetIPv4Address()
        {
            try
            {
                using (var client = new WebClient())
                {
                    return client.DownloadString("https://ipv4.icanhazip.com").Trim();
                }
            }
            catch
            {
                return "192.168.0.1"; // Тестовый IP при ошибке
            }
        }

        private static void Log(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AutoCAD_Plugin_Log.txt");

                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n");
            }
            catch
            {
                //игнорируем
            }
        }
    }
}
