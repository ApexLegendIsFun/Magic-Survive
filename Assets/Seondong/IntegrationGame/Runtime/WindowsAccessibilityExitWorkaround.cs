#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seondong.IntegrationGame
{
    // Unity 6000.3.17f1's native accessibility teardown dereferences the destroyed
    // main window (UnityPlayer + 0xb1b0a1) after a Windows UI Automation query.
    // This game has no AccessibilityHierarchy. Decline provider requests on its
    // own window, leaving Unity's normal input, rendering and quit path intact.
    internal static class WindowsAccessibilityExitWorkaround
    {
        private const uint GetObject = 0x003D;
        private static readonly UIntPtr SubclassId = new UIntPtr(0x4D534158);
        private static readonly SubclassProc Callback = WindowMessage;
        private static IntPtr window;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr SubclassProc(IntPtr hwnd, uint message, UIntPtr wParam,
            IntPtr lParam, UIntPtr id, UIntPtr data);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
        [DllImport("comctl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowSubclass(IntPtr hwnd, SubclassProc callback, UIntPtr id, UIntPtr data);
        [DllImport("comctl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveWindowSubclass(IntPtr hwnd, SubclassProc callback, UIntPtr id);
        [DllImport("comctl32.dll")]
        private static extern IntPtr DefSubclassProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Install()
        {
            if (Application.unityVersion != "6000.3.17f1" || window != IntPtr.Zero) return;
            // GetActiveWindow is scoped to the calling (Unity main) thread.
            var candidate = GetActiveWindow();
            if (candidate == IntPtr.Zero || !SetWindowSubclass(candidate, Callback, SubclassId, UIntPtr.Zero))
            {
                Debug.LogWarning("[Windows Exit] Could not install Unity accessibility workaround.");
                return;
            }
            window = candidate;
            Application.quitting += Remove;
            Debug.Log("[Windows Exit] Unity 6000.3.17f1 accessibility provider workaround installed.");
        }

        private static IntPtr WindowMessage(IntPtr hwnd, uint message, UIntPtr wParam,
            IntPtr lParam, UIntPtr id, UIntPtr data)
        {
            if (message == GetObject) return IntPtr.Zero;
            return DefSubclassProc(hwnd, message, wParam, lParam);
        }

        private static void Remove()
        {
            Application.quitting -= Remove;
            if (window != IntPtr.Zero) RemoveWindowSubclass(window, Callback, SubclassId);
            window = IntPtr.Zero;
        }
    }
}
#endif
