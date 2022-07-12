using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[InitializeOnLoad]
public static class EventSubscribingExample_RegisteringPackages
{
    public class PackageManifest
    {
        public string[] gitDependencies;
    }
    static AddRequest Request;
    private static List<string> _dependences = new List<string>();
    private static int _totalCnt;
    static EventSubscribingExample_RegisteringPackages()
    {
        Debug.Log("EventSubscribingExample_RegisteringPackages");

        // Subscribe to the event using the addition assignment operator (+=).
        // This executes the code in the handler whenever the event is fired.
        Events.registeredPackages += RegisteredPackagesEventHandler;
        Events.registeringPackages += RegisteringPackagesEventHandler;
    }
    static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
    {
        // Code executed here can safely assume that the Editor has finished compiling the new list of packages
        Debug.Log("RegisteredPackagesEventHandler");
        foreach (var addedPackage in packageRegistrationEventArgs.added)
        {
            Debug.Log($"Adding {addedPackage.displayName}, assetPath: {addedPackage.assetPath}");
            TextAsset text = (TextAsset)AssetDatabase.LoadAssetAtPath($"{addedPackage.assetPath}/package.json", typeof(TextAsset));
            if (null != text)
            {
                Debug.Log($"{text.text}");
                var manifest = JsonUtility.FromJson<PackageManifest>(text.text);
                if (null != manifest && null != manifest.gitDependencies)
                {
                    Debug.Log($"gitDependencies.Length: {manifest.gitDependencies.Length}");
                    _dependences.AddRange(manifest.gitDependencies);
                    _totalCnt = _dependences.Count;
                    EditorApplication.update += Progress;
                    RequestDependences();
                }
            }
            else
            {
                Debug.Log("Package file not find");
            }
        }
    }

    static void RequestDependences()
    {
        if (!_dependences.Any())
        {           
            EditorApplication.update -= Progress;
            EditorUtility.ClearProgressBar();
            return;
        }
        var last = _dependences[_dependences.Count - 1];
        Debug.Log($"Request Dependency: {last}");
        Request = Client.Add(last);
        EditorUtility.DisplayProgressBar("Request git dependences...", last, (_totalCnt - _dependences.Count) / (float)_totalCnt);
        _dependences.RemoveAt(_dependences.Count - 1);
    }

    static void RequestComplete()
    {
        if (Request.Status == StatusCode.Success)
            Debug.Log("Installed: " + Request.Result.packageId);
        else if (Request.Status >= StatusCode.Failure)
            Debug.Log(Request.Error.message);
        if (_dependences.Count > 0)
        {
            RequestDependences();
        }
        else
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= Progress;
        }
    }
    static void Progress()
    {
        if (Request.IsCompleted)
        {
            RequestComplete();
        }
    }
    // The method is expected to receive a PackageRegistrationEventArgs event argument.
    static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
    {
        Debug.Log("RegisteringPackagesEventHandler");

        foreach (var addedPackage in packageRegistrationEventArgs.added)
        {
            Debug.Log($"Adding {addedPackage.displayName}, assetPath: {addedPackage.assetPath}");
            TextAsset text = (TextAsset)AssetDatabase.LoadAssetAtPath($"{addedPackage.assetPath}/package.json", typeof(TextAsset));
            if (null != text)
            {
                Debug.Log($"{text.text}");
            }
            else
            {
                Debug.Log("Package file not find");
            }
        }

        foreach (var removedPackage in packageRegistrationEventArgs.removed)
        {
            Debug.Log($"Removing {removedPackage.displayName}");
        }

        // The changedFrom and changedTo collections contain the packages that are about to be updated.
        // Both collections are guaranteed to be the same size with indices matching the same package name.
        // for (int i = 0; i < packageRegistrationEventArgs.changedFrom.Count; i++)
        // {
        //     var oldPackage = packageRegistrationEventArgs.changedFrom[i];
        //     var newPackage = packageRegistrationEventArgs.changedTo[i];
        //
        //     Debug.Log($"Changing ${oldPackage.displayName} version from ${oldPackage.version} to ${newPackage.version}");
        // }
    }
}
