#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ModularProject.Editor
{
    [InitializeOnLoad]
    internal static partial class GitDependencesResolver
    {
        private class PackageManifest
        {
            public string[] gitDependencies;
        }
        private static AddRequest _addRequest;
        private static ListRequest _listRequest;
        static GitDependencesResolver()
        {
            // Test();
            // Subscribe to the event using the addition assignment operator (+=).
            Events.registeringPackages += RegisteringPackagesEventHandler;
            Events.registeredPackages += RegisteredPackagesEventHandler;
            //
            EditorApplication.update += Update;
        }

        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // List packages installed for the project
            _listRequest = Client.List();
            Debug.Log("Request installed packages");
            EditorUtility.DisplayProgressBar("List packages installed for the project", "In progress", 0.33f);
        }
        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // Code executed here can safely assume that the Editor has finished compiling the new list of packages
            foreach (var addedPackage in packageRegistrationEventArgs.added)
            {
                Debug.Log($"Adding {addedPackage.displayName}");
                var packageManifestPath = $"{addedPackage.assetPath}/package.json";
                var packageManifestAsset = (TextAsset)AssetDatabase.LoadAssetAtPath(packageManifestPath, typeof(TextAsset));
                if (null != packageManifestAsset)
                {
                    var packageManifest = JsonUtility.FromJson<PackageManifest>(packageManifestAsset.text);
                    if (null != packageManifest && null != packageManifest.gitDependencies && packageManifest.gitDependencies.Any())
                    {
                        _listRequest = Client.List();
                        Debug.Log("Request installed packages");
                        EditorUtility.DisplayProgressBar("List packages installed for the project", "In progress", 0.33f);
                        
                        Debug.Log($"Package {addedPackage.displayName} has {packageManifest.gitDependencies.Length} git dependence(s).");
                        // _gitDependencesToAdd.AddRange(packageManifest.gitDependencies.Where(path => !_installedGitDependences.Contains(path)));
                        PersistentCache.AddGitDependencesToInstall(packageManifest.gitDependencies);
                        if (null == _listRequest && null == _addRequest) RequestNextGitDependences();
                        // Debug.Log($"Ready to install git dependences, cnt: {_gitDependencestotalCnt}");
                        // EditorUtility.DisplayProgressBar("Ready to install git dependences", "Starting", 0f);
                    }
                }
                else
                {
                    Debug.LogError($"Package manifest file {packageManifestPath} not find.");
                }
            }
        }

        private static void Update()
        {
            if (null != _listRequest && _listRequest.IsCompleted)
            {
                switch (_listRequest.Status)
                {
                    case StatusCode.Success:
                    {
                        var installedGitDependences = new List<string>();
                        foreach (var package in _listRequest.Result)
                        {
                            if (PackageSource.Git != package.source) continue;
                            installedGitDependences.Add(package.repository.url);
                            // Debug.Log($"Installed package: [{package.repository.url}]");
                        }
                        PersistentCache.SetInstalledPackageList(installedGitDependences);
                        break;
                    }
                    case >= StatusCode.Failure:
                        Debug.Log(_listRequest.Error.message);
                        break;
                }
                EditorUtility.ClearProgressBar();
                _listRequest = null;
                RequestNextGitDependences();
            }

            if (null == _listRequest && null != _addRequest && _addRequest.IsCompleted)
            {
                switch (_addRequest.Status)
                {
                    case StatusCode.Success:
                        Debug.Log("Installed: " + _addRequest.Result.packageId);
                        break;
                    case >= StatusCode.Failure:
                        Debug.Log(_addRequest.Error.message);
                        break;
                }
                RequestNextGitDependences();
            }
        }

        private static void RequestNextGitDependences()
        {
            var installed = PersistentCache.InstalledPackageList;
            string gitDependence;
            do
            {
                gitDependence = PersistentCache.GetNextGitDependeceToInstall();
                if (installed.Contains(gitDependence))
                {
                    Debug.Log($"Skip {gitDependence}");
                    continue;
                }
                break;
            } while (null != gitDependence);
            if (string.IsNullOrEmpty(gitDependence)) CleanAddProgress();
            else
            {
                Debug.Log($"Request GitDependence: {gitDependence}");
                _addRequest = Client.Add(gitDependence);
                EditorUtility.DisplayProgressBar($"Request GitDependence: {gitDependence}...", gitDependence, 0.66f);
            }
        }

        private static void CleanAddProgress()
        {
            _addRequest = null;
            EditorUtility.ClearProgressBar();
        }

        private static class PersistentCache
        {
            private const string GitPackagesInstalled = "GitDependencesResolver.Installed";
            private const string GitPackagesDependence = "GitDependencesResolver.Dependence";
            public static void SetInstalledPackageList(List<string> installedList)
            {
                var sb = new StringBuilder();
                installedList.ForEach(url => sb.Append($"{url}\n"));
                sb.Remove(sb.Length - 1, 1);
                EditorPrefs.SetString(GitPackagesInstalled, sb.ToString());
            }

            public static string[] InstalledPackageList
            {
                get
                {
                    var savedString = EditorPrefs.GetString(GitPackagesInstalled);
                    if (string.IsNullOrEmpty(savedString)) return null;
                    return savedString.Split('\n');
                }
            }

            public static void AddGitDependencesToInstall(string[] dependences)
            {
                // remove dumplicated url if exist
                dependences = dependences.Distinct().ToArray();
                var sb = new StringBuilder();
                var savedString = EditorPrefs.GetString(GitPackagesDependence);
                var urlArray = savedString.Split('\n');
                if (!string.IsNullOrEmpty(savedString))
                {
                    sb.Append(savedString);
                    sb.Append('\n');
                }

                foreach (var url in dependences)
                {
                    if (urlArray.Contains(url))
                    {
                        continue;
                    }
                    sb.Append($"{url}\n");
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    EditorPrefs.SetString(GitPackagesDependence, sb.ToString());
                }
                else
                {
                    ClearGitDependencesToInstall();
                }
            }

            public static string GetNextGitDependeceToInstall()
            {
                var sb = new StringBuilder();
                var savedString = EditorPrefs.GetString(GitPackagesDependence);
                if (string.IsNullOrEmpty(savedString)) return null;
                var urlArray = savedString.Split('\n');
                for (var i = 1; i < urlArray.Length; i++)
                {
                    sb.Append($"{urlArray[i]}\n");
                }

                if (sb.Length > 0)
                {
                    EditorPrefs.SetString(GitPackagesDependence, sb.Remove(sb.Length - 1, 1).ToString());
                }
                else
                {
                    ClearGitDependencesToInstall();
                }
                return urlArray[0];
            }

            public static void ClearGitDependencesToInstall()
            {
                EditorPrefs.DeleteKey(GitPackagesDependence);
            }
        }
    }
}
#endif