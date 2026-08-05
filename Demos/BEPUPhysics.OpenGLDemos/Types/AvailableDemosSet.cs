using DemoContentLoader;
using DemoRenderer;
using System;
using System.Collections.Generic;
using BEPUPhysics.OpenGLDemos.Demos;
using BEPUPhysics.OpenGLDemos.Demos.Characters;
using BEPUPhysics.OpenGLDemos.Demos.Dancers;
using BEPUPhysics.OpenGLDemos.Demos.Sponsors;
using BEPUPhysics.OpenGLDemos.Demos.Cars;
using BEPUPhysics.OpenGLDemos.Demos.Tanks;

namespace BEPUPhysics.OpenGLDemos.Types;

/// <summary>
/// Constructs a demo from the set of available demos on demand.
/// </summary>
public class AvailableDemosSet
{
    struct Option
    {
        public string Name;
        public Func<ContentArchive, Camera, RenderSurface, DemoBase> Builder;
    }

    List<Option> options = [];
    void AddOption<T>() where T : DemoBase, new()
    {
        options.Add(new Option
        {
            Builder = (content, camera, surface) =>
            {
                // Note that the actual work is done in the Initialize function rather than a constructor.
                // The 'new T()' syntax actually uses reflection and repackages exceptions in an inconvenient way.
                // By using Initialize instead, the stack trace and debugger will go right to the source.
                T demo = new();
                demo.LoadGraphicalContent(content, surface);
                demo.Initialize(content, camera);
                return demo;
            },
            Name = typeof(T).Name
        });
    }

    public AvailableDemosSet()
    {
        AddOption<CarDemo>();
        AddOption<TankDemo>();
        AddOption<CharacterDemo>();
        AddOption<RagdollTubeDemo>();
        AddOption<PyramidDemo>();
        AddOption<ColosseumDemo>();
        AddOption<NewtDemo>();
        AddOption<ClothDemo>();
        AddOption<LagrangianDeformableStairsDemo>();
        AddOption<DancerDemo>();
        AddOption<PlumpDancerDemo>();
        AddOption<ContinuousCollisionDetectionDemo>();
        AddOption<PlanetDemo>();
        AddOption<PerBodyGravityDemo>();
        AddOption<CompoundDemo>();
        AddOption<RopeStabilityDemo>();
        AddOption<SubsteppingDemo>();
        AddOption<ChainFountainDemo>();
        AddOption<RopeTwistDemo>();
        AddOption<FrictionDemo>();
        AddOption<BouncinessDemo>();
        AddOption<RayCastingDemo>();
        AddOption<SweepDemo>();
        AddOption<ContactEventsDemo>();
        AddOption<CollisionTrackingDemo>();
        AddOption<CollisionQueryDemo>();
        AddOption<SolverContactEnumerationDemo>();
        AddOption<CustomVoxelCollidableDemo>();
        AddOption<BlockChainDemo>();
        AddOption<SponsorDemo>();
    }

    public int Count { get { return options.Count; } }

    public string GetName(int index)
    {
        return options[index].Name;
    }
    public DemoBase Build(int index, ContentArchive content, Camera camera, RenderSurface surface)
    {
        return options[index].Builder(content, camera, surface);
    }
}
