using AutoCAD.IPPlugin.Net8.Common;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

[assembly: ExtensionApplication(typeof(AutoCAD.IPPlugin.Net8.MainPlugin))]

namespace AutoCAD.IPPlugin.Net8
{
    public class MainPlugin : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\nIP Plugin initialized. Use SHOW_IP_PLUGIN_RIBBON to show ribbon.");
                }

                // Отложенное создание ленты
                Application.Idle += OnIdle;
            }
            catch (Exception ex)
            {
                //игнорируем
            }
        }

        private void OnIdle(object sender, EventArgs e)
        {
            Application.Idle -= OnIdle;
            CreateRibbonTab();
        }

        internal void CreateRibbonTab()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return;

                // Проверяем существование вкладки
                foreach (RibbonTab rTab in ribbon.Tabs)
                {
                    if (rTab.Id == "IP_PLUGIN_TAB") return;
                }

                // Новая вкладка
                var tab = new RibbonTab
                {
                    Title = "IP Plugin",
                    Id = "IP_PLUGIN_TAB",
                    IsActive = false
                };
                ribbon.Tabs.Add(tab);

                // Панель
                var panelSource = new RibbonPanelSource
                {
                    Title = "Utilities"
                };
                var panel = new RibbonPanel { Source = panelSource };
                tab.Panels.Add(panel);

                // Кнопка
                var btn = new RibbonButton
                {
                    Text = "Get IP + Load DWG",
                    ShowText = true,
                    Size = RibbonItemSize.Large,
                    Orientation = Orientation.Vertical,
                    CommandHandler = new RelayCommand(() =>
                    {
                        var doc = Application.DocumentManager.MdiActiveDocument;
                        doc?.SendStringToExecute("RUN_IP_CMD ", true, false, true);
                    })
                };

                // Иконка
                //TODO: скорректировать парсинг
                var embeddedIcon = GetEmbeddedIcon();
                if (embeddedIcon != null)
                {
                    btn.Image = embeddedIcon;
                }
                else
                {
                }

                panelSource.Items.Add(btn);
            }
            catch (Exception ex)
            {
                // игнорируем
            }
        }
        private BitmapImage GetEmbeddedIcon()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"{assembly.GetName().Name}.ip_icon.png";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Debug.WriteLine("Icon resource not found");
                        return null;
                    }

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading embedded icon: {ex.Message}");
                return null;
            }
        }

        public void Terminate() { }
    }
}