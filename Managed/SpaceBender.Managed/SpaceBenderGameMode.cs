using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    [UClass, BlueprintType, Blueprintable]
    class ASpaceBenderGameMode : AGameMode
    {
        protected override void BeginPlay()
        {
            base.BeginPlay();

            FMessage.Log(ELogVerbosity.Warning, "Hello from C# (" + this.GetType().ToString() + ":BeginPlay)");
        }
    }
}
