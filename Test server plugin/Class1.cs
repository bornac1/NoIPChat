using Server_base;
using HarmonyLib;
using Messages;
using MessagePack;
namespace Test_server_plugin
{
    public class Class1 : IPlugin
    {
        public Server? Server { get; set; }
        public bool IsPatch { get => true; }
        public void Initialize()
        {
            Console.WriteLine("Test patch executed!");
        }
    }
    public class Patch1
    {
        [HarmonyPatch(typeof(Client), "Receive")]
        public class ReceivePatch
        {
            public static bool Prefix(Client __instance, ref Task __result)
            {
                __result = Receive(__instance);
                return false;
            }
            public static async Task Receive(Client __instance)
            {
                while ((bool)typeof(Client).GetField("connected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance))
                {
                    try
                    {
                        int length = await (Task<int>)typeof(Client).GetMethod("ReadLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, null);
                        ReadOnlyMemory<byte>? data = null;
                        if (length < 1024 || (bool)typeof(Client).GetField("auth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance))
                        {
                            //Non authenticated is limited to 1024
                            data = await (Task<ReadOnlyMemory<byte>>)typeof(Client).GetMethod("ReadData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { length });
                        }
                        if (data != null)
                        {
                            Console.WriteLine("received from " + (string)typeof(Client).GetField("name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance) + (string)typeof(Client).GetField("user", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance));
                            //Print(data);
                            Console.WriteLine("end receive");
                            try
                            {
                                Message message = await Processing.Deserialize(data.Value);
                                await (Task)typeof(Client).GetMethod("ProcessMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { message });
                            }
                            catch (MessagePackSerializationException)
                            {
                                //Mewssage error
                                await __instance.Disconnect();
                            }
                            /*if (timer != null)
                            {
                                //Reset timer
                                timer.Stop();
                                timer.Start();
                            }*/
                        }
                    }
                    catch (Exception ex)
                    {
                        /*if (ex is TransportException)
                        {
                            //assume disconnection
                            //No need for logging
                        }
                        else if (ex is ObjectDisposedException)
                        {
                            //already disposed
                            //No need for logging
                        }
                        else
                        {
                            //Should be logged
                            await server.WriteLog(ex);
                        }
                        connected = false;*/
                        await __instance.Disconnect();
                    }
                }
            }
        }
    }
}
