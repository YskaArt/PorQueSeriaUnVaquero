using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu de editor para empezar de cero: borra el save (savegame.json) y todos los PlayerPrefs
/// (incluida la marca del tutorial). "Clear All PlayerPrefs" de Unity no alcanza porque el
/// progreso vive en un archivo JSON, no en PlayerPrefs.
/// </summary>
public static class ResetProgressMenu
{
    [MenuItem("Tools/Borrar progreso (save + PlayerPrefs)")]
    private static void ResetProgress()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Borrar progreso",
                "Detene el Play Mode primero: el juego vuelve a guardar al cerrarse y recrearia el save.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Borrar progreso",
                "Esto borra el save y todos los PlayerPrefs (el tutorial volvera a aparecer).\nNo se puede deshacer.",
                "Borrar", "Cancelar"))
            return;

        string path = GameSaveManager.SaveFilePath;
        bool deleted = false;
        if (File.Exists(path))
        {
            File.Delete(path);
            deleted = true;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log(deleted
            ? $"[ResetProgress] Save borrado ({path}) y PlayerPrefs limpiados."
            : $"[ResetProgress] No habia save en {path}; PlayerPrefs limpiados.");
    }

    [MenuItem("Tools/Abrir carpeta del save")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(GameSaveManager.SaveFilePath);
    }
}
