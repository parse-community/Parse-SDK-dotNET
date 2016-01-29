using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Parse.Common.Internal {
  /// <summary>
  /// This class represents the internal Unity dispatcher used by the Parse SDK.
  ///
  /// It should be initialized once in your game, usually via ParseInitializeBehavior.
  ///
  /// In certain, advanced use-cases, you may wish to use
  /// this to set up your dispatcher manually.
  /// </summary>
  // TODO: (richardross) Review this interface before going public.
  public sealed class Dispatcher {
    static Dispatcher() {
      Instance = new Dispatcher();
    }

    private Dispatcher() {
      DispatcherCoroutine = CreateDispatcherCoroutine();
    }

    public static Dispatcher Instance { get; private set; }

    public GameObject GameObject { get; set; }
    public IEnumerator DispatcherCoroutine { get; private set; }

    private readonly ReaderWriterLockSlim dispatchQueueLock = new ReaderWriterLockSlim();
    private readonly Queue<Action> dispatchQueue = new Queue<Action>();

    public void Post(Action action) {
      if (dispatchQueueLock.IsWriteLockHeld) {
        dispatchQueue.Enqueue(action);
        return;
      }

      dispatchQueueLock.EnterWriteLock();
      try {
        dispatchQueue.Enqueue(action);
      } finally {
        dispatchQueueLock.ExitWriteLock();
      }
    }

    private IEnumerator CreateDispatcherCoroutine() {
      // We must stop the first invocation here, so that we don't actually do anything until we begin looping.
      yield return null;
      while (true) {
        dispatchQueueLock.EnterUpgradeableReadLock();
        try {
          // We'll only empty what's already in the dispatch queue in this iteration (so that a
          // nested dispatch behaves like nextTick()).
          int count = dispatchQueue.Count;
          if (count > 0) {
            dispatchQueueLock.EnterWriteLock();
            try {
              while (count > 0) {
                try {
                  dispatchQueue.Dequeue()();
                } catch (Exception e) {
                  // If an exception occurs, catch it and log it so that dispatches aren't broken.
                  Debug.LogException(e);
                }
                count--;
              }
            } finally {
              dispatchQueueLock.ExitWriteLock();
            }
          }
        } finally {
          dispatchQueueLock.ExitUpgradeableReadLock();
        }
        yield return null;
      }
    }
  }
}
