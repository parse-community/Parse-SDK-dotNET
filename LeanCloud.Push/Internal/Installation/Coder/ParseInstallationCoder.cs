using System;
using System.Linq;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Core.Internal;

namespace LeanCloud.Push.Internal {
  public class AVInstallationCoder : IAVInstallationCoder {
    private static readonly AVInstallationCoder instance = new AVInstallationCoder();
    public static AVInstallationCoder Instance {
      get {
        return instance;
      }
    }
    private const string ISO8601Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

    public IDictionary<string, object> Encode(AVInstallation installation) {
      var state = installation.GetState();
      var data = PointerOrLocalIdEncoder.Instance.Encode(state.ToDictionary(x => x.Key, x => x.Value)) as IDictionary<string, object>;
      data["objectId"] = state.ObjectId;
      if (state.CreatedAt != null) {
        data["createdAt"] = state.CreatedAt.Value.ToString(ISO8601Format);
      }
      if (state.UpdatedAt != null) {
        data["updatedAt"] = state.UpdatedAt.Value.ToString(ISO8601Format);
      }
      return data;
    }

    public AVInstallation Decode(IDictionary<string, object> data) {
      var state = AVObjectCoder.Instance.Decode(data, AVDecoder.Instance);
      return AVObjectExtensions.FromState<AVInstallation>(state, "_Installation");
    }
  }
}