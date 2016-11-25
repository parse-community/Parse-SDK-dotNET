// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Common.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LeanCloud {
  /// <summary>
  /// Mandatory MonoBehaviour for scenes that use LeanCloud. Set the application ID and .NET key
  /// in the editor.
  /// </summary>
  // TODO (hallucinogen): somehow because of Push, we need this class to be added in a GameObject
  // called `AVInitializeBehaviour`. We might want to fix this.
  public class AVInitializeBehaviour : MonoBehaviour {
    private static bool isInitialized = false;

    /// <summary>
    /// The LeanCloud applicationId used in this app. You can get this value from the LeanCloud website.
    /// </summary>
    [SerializeField]
    public string applicationID;

    /// <summary>
    /// The LeanCloud dotnetKey used in this app. You can get this value from the LeanCloud website.
    /// </summary>
    [SerializeField]
    public string dotnetKey;

    [SerializeField]
    public string server;

    /// <summary>
    /// Initializes the LeanCloud SDK and begins running network requests created by LeanCloud.
    /// </summary>
    public virtual void Awake() {
      Initialize();

      // Force the name to be `AVInitializeBehaviour` in runtime.
      gameObject.name = "AVInitializeBehaviour";
    }

    public void Initialize() {
      if (isInitialized) {
        return;
      }

      isInitialized = true;
      // Keep this gameObject around, even when the scene changes.
      GameObject.DontDestroyOnLoad(gameObject);

      AVClient.Initialize(new AVClient.Configuration {
        ApplicationId = applicationID,
        WindowsKey = dotnetKey,
        Server = string.IsNullOrEmpty(server) ? null : server
      });

      Dispatcher.Instance.GameObject = gameObject;

      // Kick off the dispatcher.
      StartCoroutine(Dispatcher.Instance.DispatcherCoroutine);
    }
  }
}
