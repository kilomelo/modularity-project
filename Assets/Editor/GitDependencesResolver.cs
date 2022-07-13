using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ModularProject.Editor
{
    [InitializeOnLoad]
    internal static class GitDependencesResolver
    {
        private class PackageManifest
        {
            public string[] gitDependencies;
        }
        private static List<string> _installedGitDependences = new List<string>();
        private static int _skipedCnt;
        private static AddRequest _addRequest;
        private static ListRequest _listRequest;
        private static List<string> _gitDependencesToAdd = new List<string>();
        private static int _gitDependencestotalCnt;
        static GitDependencesResolver()
        {
            // Subscribe to the event using the addition assignment operator (+=).
            Events.registeredPackages += RegisteredPackagesEventHandler;
            // List packages installed for the project
            _listRequest = Client.List();
            EditorApplication.update += ListRequestProgress;
            EditorUtility.DisplayProgressBar("List packages installed for the project", "In progress", 0f);
        }
        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // Code executed here can safely assume that the Editor has finished compiling the new list of packages
            foreach (var addedPackage in packageRegistrationEventArgs.added)
            {
                Debug.Log($"Adding {addedPackage.displayName}");
                var packageManifestAsset = (TextAsset)AssetDatabase.LoadAssetAtPath($"{addedPackage.assetPath}/package.json", typeof(TextAsset));
                if (null != packageManifestAsset)
                {
                    var packageManifest = JsonUtility.FromJson<PackageManifest>(packageManifestAsset.text);
                    if (null != packageManifest && null != packageManifest.gitDependencies)
                    {
                        Debug.Log($"Package {addedPackage.displayName} has {packageManifest.gitDependencies.Length} git dependence(s).");
                        // _gitDependencesToAdd.AddRange(packageManifest.gitDependencies.Where(path => !_installedGitDependences.Contains(path)));
                        _gitDependencesToAdd.AddRange(packageManifest.gitDependencies);
                        if (_gitDependencesToAdd.Any())
                        {
                            _skipedCnt = 0;
                            _gitDependencestotalCnt = _gitDependencesToAdd.Count;
                            EditorApplication.update += AddRequestProgress;
                            // Debug.Log($"Ready to install git dependences, cnt: {_gitDependencestotalCnt}");
                            EditorUtility.DisplayProgressBar("Ready to install git dependences", "Starting", 0f);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Package manifest file not find.");
                }
            }
        }

        private static void RequestGitDependences()
        {
            if (!_gitDependencesToAdd.Any()) throw new InvalidOperationException("gitDependencesToAdd is empty.");
            
            var gitDependence = _gitDependencesToAdd[_gitDependencesToAdd.Count - 1];
            var countString = $"{_gitDependencestotalCnt - _gitDependencesToAdd.Count + 1} of {_gitDependencestotalCnt}";
            _gitDependencesToAdd.RemoveAt(_gitDependencesToAdd.Count - 1);
            if (_installedGitDependences.Contains(gitDependence))
            {
                _skipedCnt++;
                EditorUtility.DisplayProgressBar($"[{countString}] Request GitDependence: {gitDependence}...", gitDependence,
                (_gitDependencestotalCnt - _gitDependencesToAdd.Count) / (float)_gitDependencestotalCnt);
                if (!_gitDependencesToAdd.Any()) CleanAddProgress();
                return;
            }
            Debug.Log($"[{countString}] Request GitDependence: {gitDependence}");
            _addRequest = Client.Add(gitDependence);
            EditorUtility.DisplayProgressBar($"[{countString}] Request GitDependence: {gitDependence}...", gitDependence,
                (_gitDependencestotalCnt - _gitDependencesToAdd.Count) / (float)_gitDependencestotalCnt);
        }

        private static void RequestComplete()
        {
            if (null == _addRequest) throw new NullReferenceException("AddRequest is null.");
            switch (_addRequest.Status)
            {
                case StatusCode.Success:
                    Debug.Log("Installed: " + _addRequest.Result.packageId);
                    break;
                case >= StatusCode.Failure:
                    Debug.Log(_addRequest.Error.message);
                    break;
            }

            if (_gitDependencesToAdd.Any())
            {
                RequestGitDependences();
            }
            else
            {
                CleanAddProgress();
            }
        }

        private static void CleanAddProgress()
        {
            if (_skipedCnt > 0) Debug.Log($"{_skipedCnt} package(s) skiped.");
            _addRequest = null;
            _gitDependencesToAdd.Clear();
            EditorApplication.update -= AddRequestProgress;
            EditorUtility.ClearProgressBar();
        }

        private static void AddRequestProgress()
        {
            if (null == _listRequest || !_listRequest.IsCompleted) return;
            if (!_gitDependencesToAdd.Any()) throw new InvalidOperationException("gitDependencesToAdd is empty.");
            if (null == _addRequest)
            {
                RequestGitDependences();
                return;
            }
            if (_addRequest.IsCompleted) RequestComplete();
        }

        private static void ListRequestProgress()
        {
            if (!_listRequest.IsCompleted) return;
            switch (_listRequest.Status)
            {
                case StatusCode.Success:
                {
                    foreach (var package in _listRequest.Result)
                    {
                        if (PackageSource.Git != package.source) continue;
                        _installedGitDependences.Add(package.repository.url);
                        // Debug.Log($"Installed package: [{package.repository.url}]");
                    }
                    break;
                }
                case >= StatusCode.Failure:
                    Debug.Log(_listRequest.Error.message);
                    break;
            }
            EditorApplication.update -= ListRequestProgress;
            EditorUtility.ClearProgressBar();
        }
    }
}

