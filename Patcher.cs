using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Program
{
    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: Patcher.exe <Il2CppInterop.Generator.dll>");
                return 2;
            }

            string dll = Path.GetFullPath(args[0]);
            if (!File.Exists(dll)) throw new FileNotFoundException("Target DLL not found", dll);

            string backup = dll + ".backup";
            if (!File.Exists(backup))
            {
                File.Copy(dll, backup, true);
                Console.WriteLine("Backup created: " + backup);
            }
            else
            {
                Console.WriteLine("Backup already exists: " + backup);
            }

            string targetDir = Path.GetDirectoryName(dll)!;
            string temp = dll + ".patched.tmp";
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(targetDir);
            resolver.AddSearchDirectory(AppContext.BaseDirectory);

            var rp = new ReaderParameters
            {
                ReadSymbols = false,
                ReadWrite = false,
                AssemblyResolver = resolver
            };

            using (var asm = AssemblyDefinition.ReadAssembly(dll, rp))
            {
                var module = asm.MainModule;
                var passType = module.Types.SelectMany(AllTypes)
                    .FirstOrDefault(t => t.FullName == "Il2CppInterop.Generator.Passes.Pass16ScanMethodRefs");

                if (passType == null)
                    throw new Exception("Pass16ScanMethodRefs type not found. This DLL may use a different Il2CppInterop layout.");

                var doPass = passType.Methods.FirstOrDefault(m => m.Name == "DoPass" && m.HasBody);
                if (doPass == null)
                    throw new Exception("Pass16ScanMethodRefs.DoPass() not found.");

                Console.WriteLine("Found: " + doPass.FullName);

                if (IsAlreadyPatched(doPass))
                {
                    Console.WriteLine("Already patched.");
                    return 0;
                }

                var body = doPass.Body;
                if (body.Instructions.Count == 0)
                    throw new Exception("DoPass() has an empty body.");

                // Native IntPtr size is 4 on x86 and 8 on x64.
                // x86: return immediately, avoiding the broken XREF scanner.
                // x64: continue into the original method body unchanged.
                var originalFirst = body.Instructions[0];
                var sizeOf = Instruction.Create(OpCodes.Sizeof, module.TypeSystem.IntPtr);
                var load4 = Instruction.Create(OpCodes.Ldc_I4_4);
                var ret = Instruction.Create(OpCodes.Ret);
                var beq = Instruction.Create(OpCodes.Beq_S, ret);
                var branchOriginal = Instruction.Create(OpCodes.Br_S, originalFirst);

                body.Instructions.Insert(0, sizeOf);
                body.Instructions.Insert(1, load4);
                body.Instructions.Insert(2, beq);
                body.Instructions.Insert(3, branchOriginal);
                body.Instructions.Insert(4, ret);

                beq.Operand = ret;
                branchOriginal.Operand = originalFirst;

                asm.Write(temp);
            }

            if (!File.Exists(temp))
                throw new Exception("Temporary patched DLL was not created.");

            // Replace only after the Cecil assembly is fully closed.
            string swapBackup = dll + ".prepatch";
            try { if (File.Exists(swapBackup)) File.Delete(swapBackup); } catch { }

            File.Move(dll, swapBackup);
            try
            {
                File.Move(temp, dll);
                File.Delete(swapBackup);
            }
            catch
            {
                if (File.Exists(dll)) File.Delete(dll);
                if (File.Exists(swapBackup)) File.Move(swapBackup, dll);
                throw;
            }

            Console.WriteLine();
            Console.WriteLine("PATCH SUCCESS");
            Console.WriteLine("x86 (IntPtr size 4): XREF Pass16 is skipped.");
            Console.WriteLine("x64 (IntPtr size 8): XREF Pass16 runs normally.");
            Console.WriteLine("Backup: " + backup);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PATCH FAILED: " + ex);
            return 1;
        }
    }

    static bool IsAlreadyPatched(MethodDefinition m)
    {
        var i = m.Body.Instructions;
        if (i.Count < 5) return false;
        return i[0].OpCode.Code == Code.Sizeof &&
               i[0].Operand is TypeReference tr && tr.FullName == "System.IntPtr" &&
               i[1].OpCode.Code == Code.Ldc_I4_4 &&
               i[2].OpCode.Code == Code.Beq_S &&
               i[3].OpCode.Code == Code.Br_S &&
               i[4].OpCode.Code == Code.Ret;
    }

    static System.Collections.Generic.IEnumerable<TypeDefinition> AllTypes(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes)
            foreach (var x in AllTypes(n)) yield return x;
    }
}
