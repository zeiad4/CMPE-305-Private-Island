using System;
using UnityEditor;

namespace PrivateIsland.Editor
{
    [InitializeOnLoad]
    internal static class FocusGameViewOnPlay
    {
        static FocusGameViewOnPlay()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.delayCall += FocusGameView;
        }

        private static void FocusGameView()
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
            {
                return;
            }

            EditorWindow.GetWindow(gameViewType)?.Focus();
        }
    }
}
