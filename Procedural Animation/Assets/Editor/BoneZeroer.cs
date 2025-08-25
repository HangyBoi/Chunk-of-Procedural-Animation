using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BoneZeroer : EditorWindow
{
    private GameObject rootBone;

    // Creates a new menu item in the Unity toolbar to open this window
    [MenuItem("Tools/Gecko/Bone Zeroer")]
    public static void ShowWindow()
    {
        GetWindow<BoneZeroer>("Bone Zeroer");
    }

    // This method draws the UI for our custom editor window
    void OnGUI()
    {
        GUILayout.Label("Zero Out Bone Rotations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool will insert a 'container' parent for each bone in the hierarchy " +
            "to zero out its local rotation without changing its world orientation. " +
            "\n\nIMPORTANT: Duplicate your model before running this, as it permanently changes the hierarchy!",
            MessageType.Warning);

        // Field to drag the root of the skeleton
        rootBone = (GameObject)EditorGUILayout.ObjectField("Skeleton Root Bone", rootBone, typeof(GameObject), true);

        if (rootBone == null)
        {
            EditorGUILayout.HelpBox("Please drag the root bone of your model's skeleton here.", MessageType.Info);
            return;
        }

        // The button that starts the process
        if (GUILayout.Button("Zero Out All Bone Rotations"))
        {
            if (EditorUtility.DisplayDialog("Are you sure?",
                "This will modify the hierarchy of '" + rootBone.name + "'. " +
                "Please confirm you have a backup or are working on a duplicate.", "Proceed", "Cancel"))
            {
                ProcessHierarchy(rootBone.transform);
                Debug.Log("Bone hierarchy processed successfully!");
            }
        }
    }

    // This is the main recursive function that does the work
    private void ProcessHierarchy(Transform bone)
    {
        // --- The 'Container Parent' Trick ---

        // 1. Get the original parent before we change anything.
        Transform originalParent = bone.parent;

        // 2. Create the container.
        GameObject container = new GameObject(bone.name + "_Container");
        Transform containerTransform = container.transform;

        // 3. Position the container exactly where the bone is.
        containerTransform.SetParent(originalParent);
        containerTransform.position = bone.position;
        containerTransform.rotation = bone.rotation;
        containerTransform.localScale = bone.localScale;

        // 4. Make the original bone a child of the container.
        // Its local values will now be zeroed out relative to the container.
        bone.SetParent(containerTransform);

        // --- Recurse for all children ---

        // We must copy the children to a temporary list first, because we are
        // about to change their parent, which would modify the collection we are looping through.
        List<Transform> children = new List<Transform>();
        foreach (Transform child in bone)
        {
            children.Add(child);
        }

        // Now, recursively call this function for each child.
        // Their new parent will be the original bone, which is correct.
        foreach (Transform child in children)
        {
            ProcessHierarchy(child);
        }
    }
}