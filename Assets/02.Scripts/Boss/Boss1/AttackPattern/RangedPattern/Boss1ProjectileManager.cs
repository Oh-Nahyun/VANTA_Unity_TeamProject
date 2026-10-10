using System.Collections.Generic;
using UnityEngine;

public class Boss1ProjectileManager : MonoBehaviour
{
    private readonly HashSet<GameObject> _projectiles = new();

    public void RegisterProjectile(GameObject projectile)
    {
        if (projectile == null)
        {
            return;
        }
        
        _projectiles.Add(projectile);
    }

    public void UnregisterProjectile(GameObject projectile)
    {
        _projectiles.Remove(projectile);
    }
    
    public void DestroyAllProjectiles()
    {
        foreach (GameObject projectile in _projectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile);
            }
        }
        _projectiles.Clear();
    }
    
    public void DisappearAllProjectiles()
    {
        foreach (GameObject projectile in _projectiles)
        {
            if (projectile == null)
            {
                continue;
            }

            if (projectile.TryGetComponent(out Boss1Bullet bullet))
            {
                bullet.ForceDisappear();
            }
            else if (projectile.TryGetComponent(out Boss1SplitBullet splitBullet))
            {
                splitBullet.ForceDisappear();
            }
            else
            {
                Destroy(projectile);
            }
        }

        _projectiles.Clear();
    }
}
