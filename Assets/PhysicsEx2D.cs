using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public static class PhysicsEx2D
{
    #region Static Cache
    private static List<RaycastHit2D> s_raycastHit2Ds;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InitRaycastHit2Ds() { s_raycastHit2Ds ??= new List<RaycastHit2D>(64); }
    private static List<Collider2D> s_collider2Ds;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InitCollider2Ds() { s_collider2Ds ??= new List<Collider2D>(64); }
    private static List<ContactPoint2D> s_contact2Ds;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InitContact2Ds() { s_contact2Ds ??= new List<ContactPoint2D>(64); }
    #endregion

    #region PhysicsScene Extensions
    public static IReadOnlyList<RaycastHit2D> BoxCastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitRaycastHit2Ds();
        scene.BoxCast(origin, size, angle, direction, distance, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> BoxCastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter)
    {
        InitRaycastHit2Ds();
        scene.BoxCast(origin, size, angle, direction, distance, contactFilter, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> CapsuleCastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitRaycastHit2Ds();
        scene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> CapsuleCastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter)
    {
        InitRaycastHit2Ds();
        scene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> CircleCastAll(this PhysicsScene2D scene, Vector2 origin, float radius, Vector2 direction, float distance, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitRaycastHit2Ds();
        scene.CircleCast(origin, radius, direction, distance, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> CircleCastAll(this PhysicsScene2D scene, Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter)
    {
        InitRaycastHit2Ds();
        scene.CircleCast(origin, radius, direction, distance, contactFilter, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> LineCastAll(this PhysicsScene2D scene, Vector2 start, Vector2 end, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitRaycastHit2Ds();
        scene.Linecast(start, end, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> LineCastAll(this PhysicsScene2D scene, Vector2 start, Vector2 end, ContactFilter2D contactFilter)
    {
        InitRaycastHit2Ds();
        scene.Linecast(start, end, contactFilter, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapAreaAll(this PhysicsScene2D scene, Vector2 pointA, Vector2 pointB, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        scene.OverlapArea(pointA, pointB, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapAreaAll(this PhysicsScene2D scene, Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        scene.OverlapArea(pointA, pointB, contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapBoxAll(this PhysicsScene2D scene, Vector2 point, Vector2 size, float angle, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        scene.OverlapBox(point, size, angle, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapBoxAll(this PhysicsScene2D scene, Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        scene.OverlapBox(point, size, angle, contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapCapsuleAll(this PhysicsScene2D scene, Vector2 point, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        scene.OverlapCapsule(point, size, capsuleDirection, angle, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapCapsuleAll(this PhysicsScene2D scene, Vector2 point, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        scene.OverlapCapsule(point, size, capsuleDirection, angle, contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapCircleAll(this PhysicsScene2D scene, Vector2 point, float radius, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        scene.OverlapCircle(point, radius, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapCircleAll(this PhysicsScene2D scene, Vector2 point, float radius, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        scene.OverlapCircle(point, radius, contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapPointAll(this PhysicsScene2D scene, Vector2 point, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        scene.OverlapPoint(point, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapPointAll(this PhysicsScene2D scene, Vector2 point, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        scene.OverlapPoint(point, contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> RaycastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 direction, float distance, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitRaycastHit2Ds();
        scene.Raycast(origin, direction, distance, new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<RaycastHit2D> RaycastAll(this PhysicsScene2D scene, Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter)
    {
        InitRaycastHit2Ds();
        scene.Raycast(origin, direction, distance, contactFilter, s_raycastHit2Ds);
        return s_raycastHit2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapColliderAll(this Rigidbody2D body, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        body.OverlapCollider(contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapColliderAll(this Rigidbody2D body, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        body.OverlapCollider(new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<ContactPoint2D> GetContactsAll(this Rigidbody2D body, ContactFilter2D contactFilter)
    {
        InitContact2Ds();
        body.GetContacts(contactFilter, s_contact2Ds);
        return s_contact2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapColliderAll(this Collider2D body, ContactFilter2D contactFilter)
    {
        InitCollider2Ds();
        body.OverlapCollider(contactFilter, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<Collider2D> OverlapColliderAll(this Collider2D body, int layerMask = Physics2D.DefaultRaycastLayers, bool includeTrigger = false)
    {
        InitCollider2Ds();
        body.OverlapCollider(new ContactFilter2D
        {
            useTriggers = includeTrigger,
            useLayerMask = true,
            layerMask = layerMask
        }, s_collider2Ds);
        return s_collider2Ds;
    }

    public static IReadOnlyList<ContactPoint2D> GetContactsAll(this Collider2D body, ContactFilter2D contactFilter)
    {
        InitContact2Ds();
        body.GetContacts(contactFilter, s_contact2Ds);
        return s_contact2Ds;
    }

    public static IReadOnlyList<Collider2D> GetAttachedColliders(this Rigidbody2D body)
    {
        InitCollider2Ds();
        body.GetAttachedColliders(s_collider2Ds);
        return s_collider2Ds;
    }
    #endregion
}
