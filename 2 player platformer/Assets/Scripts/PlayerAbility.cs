using System;
using UnityEngine;

public abstract class PlayerAbility : MonoBehaviour {
    public virtual void TryActivate() {
        if (CanUseNow()) {
            Activate();
        }
    }
    protected abstract void Activate();
    public abstract bool CanUseNow();

    public virtual bool overridingMaxSpeed() {
        return false;
    }
}