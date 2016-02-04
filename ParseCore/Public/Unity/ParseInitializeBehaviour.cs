// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Parse {
  /// <summary>
  /// Mandatory MonoBehaviour for scenes that use Parse. Set the application ID and .NET key
  /// in the editor.
  /// </summary>
  // TODO (hallucinogen): somehow because of Push, we need this class to be added in a GameObject
  // called `ParseInitializeBehaviour`. We might want to fix this.
  public class ParseInitializeBehaviour : MonoBehaviour {
    private static bool isInitialized = false;

    /// <summary>
    /// The Parse applicationId used in this app. You can get this value from the Parse website.
    /// </summary>
    [SerializeField]
    public string applicationID;

    /// <summary>
    /// The Parse dotnetKey used in this app. You can get this value from the Parse website.
    /// </summary>
    [SerializeField]
    public string dotnetKey;

    [SerializeField]
    public string server;

    /// <summary>
    /// Initializes the Parse SDK and begins running network requests created by Parse.
    /// </summary>
    public virtual void Awake() {
      Initialize();

      // Force the name to be `ParseInitializeBehaviour` in runtime.
      gameObject.name = "ParseInitializeBehaviour";
    }

    public void Initialize() {
      if (isInitialized) {
        return;
      }

      isInitialized = true;
      // Keep this gameObject around, even when the scene changes.
      GameObject.DontDestroyOnLoad(gameObject);

      ParseClient.Initialize(new ParseClient.Configuration {
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
