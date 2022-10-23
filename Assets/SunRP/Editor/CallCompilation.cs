using SunRP.Runtime;
using UnityEditor;

namespace SunRP
{
    public static class CallCompilation
    {
        [MenuItem("Test/Compilation")]
        public static void Compilation()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}
