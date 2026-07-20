# BEPUPhysics

## TODO

- [ ] Test large fragmented static collision of world (test inactive status once submission). If this works, then for any arbitrary large open world scene, we don't need to worry about making specialized collision bodies, and can just fragment things small enough (e.g. for walking simulation) as triangle mesh (like we did with Godot version of Methodox Walking Simulator, but more optimized).
- [ ] Create dedicated Divooka wrapper for BEPU use, e.g. `Divooka.BEPUPhysics`; This can include application level preset constraint types and simulation helpers.

## Notes

* (20260716) Note BEPUPhysics is first and foremost a constraint solver. As such it can be used to simulate limited by variant range of physics effects, from solids to soft bodies - even springs and helical motions can be modeled this way - but not exact science like FEA or fluid.
