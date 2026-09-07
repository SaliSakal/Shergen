namespace Shergen.Lua
{
    // Třída pro uchovávání callbacků v rámci LuaManager
    /// <summary>
    /// Class for storing callbacks within LuaManager
    /// </summary>
    public class RegistryCallbacks
    {
        // Slovník pro uchování callbacků (klíč: jméno funkce, hodnota: delegate)
        // Dictionary for storing callbacks (key: function name, value: delegate)
        public readonly Dictionary<string, LuaFunctionDelegate> Callbacks = new Dictionary<string, LuaFunctionDelegate>();
        private HashSet<string> registeredFunctions = new HashSet<string>();
        private Dictionary<string, object> registeredConstants = new Dictionary<string, object>();

        // Registrace Callbacků pro Lua a uložení jejich delegátů do slovníku, aby to GC neuvolnilo.
        /// <summary>
        /// Registers a callback in Lua and stores the delegate in a dictionary,
        /// so that the GC doesn't free it.
        /// </summary>
        /// <param name="lua">Lua engine instance (E.g. _lua)</param>
        /// <param name="name">The function name under which it will be registered in Lua</param>
        /// <param name="callback">Callback that will be called from Lua</param>
        public void RegisterCallback(Native lua, string name, LuaFunctionDelegate callback)
        {

            if (!callback.Method.IsStatic)
            {
                // Callbeck musí být statický
                // Callback must be static
                Console.WriteLine($"❌ Error - Callback '{name}' must be static.");
                return;
            }
            // Uložíní delegate, aby byl uchován a neuvolněn GC
            // Store the delegate so that it is preserved and not freed by the GC
            if (Callbacks.ContainsKey(name))
            {
                Callbacks[name] = callback;
                //Console.WriteLine($"📑 Registrovaná LUA Funkce {name} Aktualizována");
            }
            else
            {
                Callbacks.Add(name, callback);
                registeredFunctions.Add(name);
            }

            // Registrace funkce do Lua
            // Register the function in Lua
            lua.RegisterFunction(name, callback);
        }

        public void RegisterGlobalConstant(Native lua, string name, object value)
        {
            if (value.GetType().IsEnum)
                value = Convert.ToInt32(value); // 🚀 Převod enumu na int - Lua nepodporuje enumy přímo / Convert enum to int - Lua does not support enums directly

            if (registeredConstants.ContainsKey(name))
                registeredConstants[name] = value;
            else
                registeredConstants.Add(name, value);

            lua.RegisterGlobalConstant(name, value);
        }

        public void clearRegister()
        {
            registeredConstants.Clear(); // Vymaže všechny konstanty / Clears all constants
            registeredFunctions.Clear(); // Vymaže všechny registrované funkce / Clears all registered functions
            Callbacks.Clear();
        }

        public int ListCSFunctions(nint L)
        {
            // Seznam registrovaných C# funkcí:
            // List of registered C# functions:
            Console.WriteLine("📜 List of registered C# functions:");
            foreach (var func in registeredFunctions.ToList())
            {
                Console.WriteLine(" 🔹 " + func);
            }
            return 0;
        }

        public int ListCSConstants(nint L)
        {
            // Seznam registrovaných C# konstant:
            // List of registered C# constants:
            Console.WriteLine("📜 List of registered C# constants:");
            foreach (var kvp in registeredConstants.ToList())
            {
                Console.WriteLine($" 🔹 {kvp.Key} = {kvp.Value}");
            }
            return 0;
        }



    }
}

