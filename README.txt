Il2CppInterop x86 XREF Patcher - Universal

Purpose:
fixes XrefScanUtilFinder.FindByteWriteTargetRightAfterCallTo(...)System.OverflowException
Disables Il2CppInterop Pass16 XREF scanning only on 32-bit (x86) processes.
64-bit behavior remains unchanged.

Requirements:
- Windows
- .NET SDK 10.x or newer
- Mono.Cecil.dll from the SAME Il2CppInterop/MelonLoader installation as the target generator DLL

Usage:
1. Extract this ZIP to a normal folder. Do not run it from inside WinRAR/7-Zip.
2. Run BuildAndPatch.bat.
3. Enter the full path to Il2CppInterop.Generator.dll.
4. Enter the full path to Mono.Cecil.dll, or press Enter if it is next to the generator DLL.
5. The patcher creates Il2CppInterop.Generator.dll.backup before modifying the DLL.

Do not use this on a generator DLL from an unrelated Il2CppInterop version. It is intended for the 1.5.x generator layout used by MelonLoader 0.7.x, and the patcher checks for Pass16ScanMethodRefs.DoPass before changing anything.

To restore the original:
Delete the patched DLL and rename/copy the .backup file back to Il2CppInterop.Generator.dll.

Is this a virus?
No,the full code is source-available and it does not fetch additional packages
