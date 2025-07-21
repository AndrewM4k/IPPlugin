using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Exception = System.Exception;

[assembly: CommandClass(typeof(AutoCAD.IPPlugin.Net8.Commands.RibbonCommands))]

namespace AutoCAD.IPPlugin.Net8.Commands
{

    public static class RibbonCommands
    {
        [CommandMethod("SHOW_IP_PLUGIN_RIBBON")]
        public static void ShowRibbon()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return;

                foreach (RibbonTab tab in ribbon.Tabs)
                {
                    if (tab.Id == "IP_PLUGIN_TAB")
                    {
                        tab.IsVisible = true;
                        tab.IsActive = true;
                        return;
                    }
                }

                // Если вкладка не найдена
                new MainPlugin().CreateRibbonTab();
                ShowRibbon(); // Повторная попытка
            }
            catch (Exception ex)
            {
                //игнорируем
            }
        }
    }
}
