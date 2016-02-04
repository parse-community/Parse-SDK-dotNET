// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using Parse.Core.Internal;
using Parse.Common.Internal;

namespace Parse {
  /// <summary>
  /// A ParseACL is used to control which users and roles can access or modify a particular object. Each
  /// <see cref="ParseObject"/> can have its own ParseACL. You can grant read and write permissions
  /// separately to specific users, to groups of users that belong to roles, or you can grant permissions
  /// to "the public" so that, for example, any user could read a particular object but only a particular
  /// set of users could write to that object.
  /// </summary>
  public class ParseACL : IJsonConvertible {
    private enum AccessKind {
      Read,
      Write
    }
    private const string publicName = "*";
    private readonly ICollection<string> readers = new HashSet<string>();
    private readonly ICollection<string> writers = new HashSet<string>();

    internal ParseACL(IDictionary<string, object> jsonObject) {
      readers = new HashSet<string>(from pair in jsonObject
                                    where ((IDictionary<string, object>)pair.Value).ContainsKey("read")
                                    select pair.Key);
      writers = new HashSet<string>(from pair in jsonObject
                                    where ((IDictionary<string, object>)pair.Value).ContainsKey("write")
                                    select pair.Key);
    }

    /// <summary>
    /// Creates an ACL with no permissions granted.
    /// </summary>
    public ParseACL() {
    }

    /// <summary>
    /// Creates an ACL where only the provided user has access.
    /// </summary>
    /// <param name="owner">The only user that can read or write objects governed by this ACL.</param>
    public ParseACL(ParseUser owner) {
      SetReadAccess(owner, true);
      SetWriteAccess(owner, true);
    }

    IDictionary<string, object> IJsonConvertible.ToJSON() {
      var result = new Dictionary<string, object>();
      foreach (var user in readers.Union(writers)) {
        var userPermissions = new Dictionary<string, object>();
        if (readers.Contains(user)) {
          userPermissions["read"] = true;
        }
        if (writers.Contains(user)) {
          userPermissions["write"] = true;
        }
        result[user] = userPermissions;
      }
      return result;
    }

    private void SetAccess(AccessKind kind, string userId, bool allowed) {
      if (userId == null) {
        throw new ArgumentException("Cannot set access for an unsaved user or role.");
      }
      ICollection<string> target = null;
      switch (kind) {
        case AccessKind.Read:
          target = readers;
          break;
        case AccessKind.Write:
          target = writers;
          break;
        default:
          throw new NotImplementedException("Unknown AccessKind");
      }
      if (allowed) {
        target.Add(userId);
      } else {
        target.Remove(userId);
      }
    }

    private bool GetAccess(AccessKind kind, string userId) {
      if (userId == null) {
        throw new ArgumentException("Cannot get access for an unsaved user or role.");
      }
      switch (kind) {
        case AccessKind.Read:
          return readers.Contains(userId);
        case AccessKind.Write:
          return writers.Contains(userId);
        default:
          throw new NotImplementedException("Unknown AccessKind");
      }
    }

    /// <summary>
    /// Gets or sets whether the public is allowed to read this object.
    /// </summary>
    public bool PublicReadAccess {
      get {
        return GetAccess(AccessKind.Read, publicName);
      }
      set {
        SetAccess(AccessKind.Read, publicName, value);
      }
    }

    /// <summary>
    /// Gets or sets whether the public is allowed to write this object.
    /// </summary>
    public bool PublicWriteAccess {
      get {
        return GetAccess(AccessKind.Write, publicName);
      }
      set {
        SetAccess(AccessKind.Write, publicName, value);
      }
    }

    /// <summary>
    /// Sets whether the given user id is allowed to read this object.
    /// </summary>
    /// <param name="userId">The objectId of the user.</param>
    /// <param name="allowed">Whether the user has permission.</param>
    public void SetReadAccess(string userId, bool allowed) {
      SetAccess(AccessKind.Read, userId, allowed);
    }

    /// <summary>
    /// Sets whether the given user is allowed to read this object.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="allowed">Whether the user has permission.</param>
    public void SetReadAccess(ParseUser user, bool allowed) {
      SetReadAccess(user.ObjectId, allowed);
    }

    /// <summary>
    /// Sets whether the given user id is allowed to write this object.
    /// </summary>
    /// <param name="userId">The objectId of the user.</param>
    /// <param name="allowed">Whether the user has permission.</param>
    public void SetWriteAccess(string userId, bool allowed) {
      SetAccess(AccessKind.Write, userId, allowed);
    }

    /// <summary>
    /// Sets whether the given user is allowed to write this object.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="allowed">Whether the user has permission.</param>
    public void SetWriteAccess(ParseUser user, bool allowed) {
      SetWriteAccess(user.ObjectId, allowed);
    }

    /// <summary>
    /// Gets whether the given user id is *explicitly* allowed to read this object.
    /// Even if this returns false, the user may still be able to read it if
    /// PublicReadAccess is true or a role that the user belongs to has read access.
    /// </summary>
    /// <param name="userId">The user objectId to check.</param>
    /// <returns>Whether the user has access.</returns>
    public bool GetReadAccess(string userId) {
      return GetAccess(AccessKind.Read, userId);
    }

    /// <summary>
    /// Gets whether the given user is *explicitly* allowed to read this object.
    /// Even if this returns false, the user may still be able to read it if
    /// PublicReadAccess is true or a role that the user belongs to has read access.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns>Whether the user has access.</returns>
    public bool GetReadAccess(ParseUser user) {
      return GetReadAccess(user.ObjectId);
    }

    /// <summary>
    /// Gets whether the given user id is *explicitly* allowed to write this object.
    /// Even if this returns false, the user may still be able to write it if
    /// PublicReadAccess is true or a role that the user belongs to has write access.
    /// </summary>
    /// <param name="userId">The user objectId to check.</param>
    /// <returns>Whether the user has access.</returns>
    public bool GetWriteAccess(string userId) {
      return GetAccess(AccessKind.Write, userId);
    }

    /// <summary>
    /// Gets whether the given user is *explicitly* allowed to write this object.
    /// Even if this returns false, the user may still be able to write it if
    /// PublicReadAccess is true or a role that the user belongs to has write access.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns>Whether the user has access.</returns>
    public bool GetWriteAccess(ParseUser user) {
      return GetWriteAccess(user.ObjectId);
    }

    /// <summary>
    /// Sets whether users belonging to the role with the given <paramref name="roleName"/>
    /// are allowed to read this object.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="allowed">Whether the role has access.</param>
    public void SetRoleReadAccess(string roleName, bool allowed) {
      SetAccess(AccessKind.Read, "role:" + roleName, allowed);
    }

    /// <summary>
    /// Sets whether users belonging to the given role are allowed to read this object.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="allowed">Whether the role has access.</param>
    public void SetRoleReadAccess(ParseRole role, bool allowed) {
      SetRoleReadAccess(role.Name, allowed);
    }

    /// <summary>
    /// Gets whether users belonging to the role with the given <paramref name="roleName"/>
    /// are allowed to read this object. Even if this returns false, the role may still be
    /// able to read it if a parent role has read access.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>Whether the role has access.</returns>
    public bool GetRoleReadAccess(string roleName) {
      return GetAccess(AccessKind.Read, "role:" + roleName);
    }

    /// <summary>
    /// Gets whether users belonging to the role are allowed to read this object.
    /// Even if this returns false, the role may still be able to read it if a
    /// parent role has read access.
    /// </summary>
    /// <param name="role">The name of the role.</param>
    /// <returns>Whether the role has access.</returns>
    public bool GetRoleReadAccess(ParseRole role) {
      return GetRoleReadAccess(role.Name);
    }

    /// <summary>
    /// Sets whether users belonging to the role with the given <paramref name="roleName"/>
    /// are allowed to write this object.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="allowed">Whether the role has access.</param>
    public void SetRoleWriteAccess(string roleName, bool allowed) {
      SetAccess(AccessKind.Write, "role:" + roleName, allowed);
    }

    /// <summary>
    /// Sets whether users belonging to the given role are allowed to write this object.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="allowed">Whether the role has access.</param>
    public void SetRoleWriteAccess(ParseRole role, bool allowed) {
      SetRoleWriteAccess(role.Name, allowed);
    }

    /// <summary>
    /// Gets whether users belonging to the role with the given <paramref name="roleName"/>
    /// are allowed to write this object. Even if this returns false, the role may still be
    /// able to write it if a parent role has write access.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>Whether the role has access.</returns>
    public bool GetRoleWriteAccess(string roleName) {
      return GetAccess(AccessKind.Write, "role:" + roleName);
    }

    /// <summary>
    /// Gets whether users belonging to the role are allowed to write this object.
    /// Even if this returns false, the role may still be able to write it if a
    /// parent role has write access.
    /// </summary>
    /// <param name="role">The name of the role.</param>
    /// <returns>Whether the role has access.</returns>
    public bool GetRoleWriteAccess(ParseRole role) {
      return GetRoleWriteAccess(role.Name);
    }
  }
}
