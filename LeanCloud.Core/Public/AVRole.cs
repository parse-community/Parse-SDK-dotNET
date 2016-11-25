// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LeanCloud {
  /// <summary>
  /// Represents a Role on the LeanCloud server. ParseRoles represent groupings
  /// of <see cref="ParseUser"/>s for the purposes of granting permissions (e.g.
  /// specifying a <see cref="ParseACL"/> for a <see cref="ParseObject"/>. Roles
  /// are specified by their sets of child users and child roles, all of which are granted
  /// any permissions that the parent role has.
  ///
  /// Roles must have a name (that cannot be changed after creation of the role),
  /// and must specify an ACL.
  /// </summary>
  [AVClassName("_Role")]
  public class AVRole : AVObject {
    private static readonly Regex namePattern = new Regex("^[0-9a-zA-Z_\\- ]+$");

    /// <summary>
    /// Constructs a new AVRole. You must assign a name and ACL to the role.
    /// </summary>
    public AVRole() : base() { }

    /// <summary>
    /// Constructs a new AVRole with the given name.
    /// </summary>
    /// <param name="name">The name of the role to create.</param>
    /// <param name="acl">The ACL for this role. Roles must have an ACL.</param>
    public AVRole(string name, AVACL acl)
      : this() {
      Name = name;
      ACL = acl;
    }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    [AVFieldName("name")]
    public string Name {
      get { return GetProperty<string>("Name"); }
      set { SetProperty(value, "Name"); }
    }

    /// <summary>
    /// Gets the <see cref="ParseRelation{ParseUser}"/> for the <see cref="ParseUser"/>s that are
    /// direct children of this role. These users are granted any privileges that
    /// this role has been granted (e.g. read or write access through ACLs). You can
    /// add or remove child users from the role through this relation.
    /// </summary>
    [AVFieldName("users")]
    public AVRelation<AVUser> Users {
      get { return GetRelationProperty<AVUser>("Users"); }
    }

    /// <summary>
    /// Gets the <see cref="ParseRelation{ParseRole}"/> for the <see cref="ParseRole"/>s that are
    /// direct children of this role. These roles' users are granted any privileges that
    /// this role has been granted (e.g. read or write access through ACLs). You can
    /// add or remove child roles from the role through this relation.
    /// </summary>
    [AVFieldName("roles")]
    public AVRelation<AVRole> Roles {
      get { return GetRelationProperty<AVRole>("Roles"); }
    }

    internal override void OnSettingValue(ref string key, ref object value) {
      base.OnSettingValue(ref key, ref value);
      if (key == "name") {
        if (ObjectId != null) {
          throw new InvalidOperationException(
              "A role's name can only be set before it has been saved.");
        }
        if (!(value is string)) {
          throw new ArgumentException("A role's name must be a string.", "value");
        }
        if (!namePattern.IsMatch((string)value)) {
          throw new ArgumentException(
              "A role's name can only contain alphanumeric characters, _, -, and spaces.",
              "value");
        }
      }
    }

    /// <summary>
    /// Gets a <see cref="ParseQuery{ParseRole}"/> over the Role collection.
    /// </summary>
    public static AVQuery<AVRole> Query {
      get {
        return new AVQuery<AVRole>();
      }
    }
  }
}
