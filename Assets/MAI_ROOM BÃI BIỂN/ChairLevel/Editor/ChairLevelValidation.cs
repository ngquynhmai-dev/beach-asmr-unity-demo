using System;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Mai.Beach.Editor
{
    /// <summary>Exercises production pointer/tool handlers, without bypassing step gates.</summary>
    public static class ChairLevelValidation
    {
        private static void Require(bool value,string message)
        {if(!value)throw new InvalidOperationException("Chair QA: "+message);}

        private static void Sweep(ChairCleaningLevel level)
        {
            for(int y=0;y<=40;y++)for(int x=0;x<=40;x++)
            {var p=new Vector2(x/40f,y/40f);level.PointerBeginUV(p);level.PointerMoveUV(p);level.PointerEnd();}
        }

        [MenuItem("MAI/Chair Level/Validate 13-step playthrough")]
        public static void RunMenu(){Debug.Log(Run());}

        public static string Run(bool restartAfter=true)
        {
            Require(Application.isPlaying,"Enter Play Mode first.");
            var level=UnityEngine.Object.FindObjectOfType<ChairCleaningLevel>();
            Require(level!=null && level.IsReady,"Level must be initialized.");
            var log=new StringBuilder();level.RestartLevel();
            var basket=UnityEngine.Object.FindObjectOfType<RescueBasket>();
            Require(basket!=null && basket.Ready && basket.Count==0,"A real empty basket must be initialized.");
            Require(basket.MouthContains(new Vector2(.5f,.46f)),"Basket sand interior accepts drops.");
            Require(!basket.MouthContains(new Vector2(.5f,.18f)) && !basket.MouthContains(Vector2.zero),"Front wall and outside basket reject drops.");
            Require(level.StepTotal==13,"Exactly thirteen steps required.");
            Require(!level.PointerBeginUV(new Vector2(.27f,.82f)),"No action before choosing tool.");
            level.SelectTool(0);
            Require(!level.PointerBeginUV(new Vector2(.56f,.55f)),"Crab cannot be collected by hand.");
            Require(level.RescuedCount==0,"Wrong tool must not collect animals.");
            level.SelectTool(1);Require(level.CrabTongsVisible,"Crab tongs must be visibly rendered when selected.");
            Require(level.PointerBeginUV(new Vector2(.56f,.55f)),"Tongs grasp crab.");level.AdvanceSimulation(.2f);
            Require(level.CrabTongsGrip>.99f,"Tongs close while holding crab.");level.ReleaseAnimal(false);level.PointerEnd();level.AdvanceSimulation(.4f);
            Require(level.CrabTongsGrip<.01f,"Tongs reopen after release.");level.SelectTool(0);
            var homes=new[]{new Vector2(.27f,.82f),new Vector2(.40f,.68f),new Vector2(.40f,.40f),new Vector2(.76f,.43f),new Vector2(.56f,.55f)};
            level.PointerBeginUV(homes[0]);Require(!level.ReleaseAnimal(false),"Dropping outside tray must return animal.");level.PointerEnd();
            Require(level.RescuedCount==0,"Failed drop must not count.");
            for(int i=0;i<5;i++)
            {level.SelectTool(i==4?1:0);Require(level.PointerBeginUV(homes[i]),"Animal pickup "+i);Require(level.ReleaseAnimal(true),"Animal tray drop "+i);level.PointerEnd();}
            Require(basket.Count==5,"All rescued animals must remain in the basket.");
            basket.SettleForValidation();for(int i=0;i<5;i++)Require(basket.IsSettledInside(i),"Animal "+i+" must stay visible inside the mouth and behind the front rim.");
            Require(level.RescuedCount==5,"All four marine snails and the crab rescued.");level.AdvanceSimulation(.8f);
            Require(level.CurrentStep==1,"Rescue advances to seaweed removal.");log.AppendLine("PASS 01: correct tools, failed drops, 4 snails + 1 crab visible inside layered basket; outer wall rejected.");
            for(int step=1;step<=11;step++)
            {
                Require(level.CurrentStep==step,"Unexpected step before "+(step+1));
                level.PointerBeginUV(new Vector2(.4f,.5f));level.PointerEnd();Require(level.Progress==0,"Tool selection required at step "+step);
                level.SelectTool(0);level.PointerBeginUV(new Vector2(-1,-1));level.PointerMoveUV(new Vector2(-1,-1));level.PointerEnd();
                Require(level.Progress==0,"Outside surface must not progress.");
                if(step==1)
                {
                    level.PointerBeginUV(new Vector2(.5f,.5f));level.PointerMoveUV(new Vector2(.55f,.5f));level.PointerEnd();
                    Require(level.RemovedKelpCount==0 && level.Progress==0,"Rubbing the chair must not erase kelp.");
                    var kelpHomes=new[]{new Vector2(.16f,.81f),new Vector2(.82f,.30f)};
                    Require(level.PointerBeginUV(kelpHomes[0]),"Kelp can be grasped as a separate sprite.");
                    Require(!level.ReleaseKelp(false),"Kelp drop outside its basket rejected.");level.PointerEnd();
                    for(int k=0;k<2;k++){Require(level.PointerBeginUV(kelpHomes[k]),"Kelp pickup "+k);Require(level.ReleaseKelp(true),"Kelp basket drop "+k);level.PointerEnd();}
                    Require(level.RemovedKelpCount==2,"Both kelp clumps must be physically moved.");
                }
                else if(step==7)
                {
                    var stains=new[]{new Vector2(.32f,.87f),new Vector2(.44f,.73f),new Vector2(.53f,.44f)};
                    foreach(var p in stains){level.PointerBeginUV(p);level.PointerEnd();}
                    level.SelectTool(1);foreach(var p in stains){level.PointerBeginUV(p);level.PointerEnd();}
                    Require(level.Progress==0,"Stain solvent must soak before wiping.");
                    level.AdvanceSimulation(1.2f);
                    var fields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                    var soapPixels=(Color[])typeof(ChairCleaningLevel).GetField("_pixels",fields).GetValue(level);
                    var build=(Color[])typeof(ChairCleaningLevel).GetField("_brushPixels",fields).GetValue(level);
                    int dimension=Mathf.RoundToInt(Mathf.Sqrt(build.Length));int cell=Mathf.RoundToInt(stains[0].y*dimension)*dimension+Mathf.RoundToInt(stains[0].x*dimension);
                    float soapBefore=soapPixels[cell].g,buildBefore=build[cell].r;
                    level.PointerBeginUV(stains[0]);for(int k=0;k<12;k++)level.PointerMoveUV(stains[0]);level.PointerEnd();
                    Require(soapPixels[cell].g>=soapBefore && build[cell].r>=buildBefore && (buildBefore>=.99f || build[cell].r>buildBefore),"Sponge must retain and build foam up to full density, not erase a hole.");
                    foreach(var p in stains){level.PointerBeginUV(p);level.PointerEnd();}
                }
                else
                {
                    Sweep(level);
                    if(step==11){Require(level.Progress>=.5f && level.Progress<1,"Drying requires separate frame wipe.");level.SelectTool(1);Sweep(level);}
                }
                Require(level.Progress>=1,"Coverage did not complete step "+(step+1));
                if(step==3)
                {
                    var fields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                    var sandMask=(Color[])typeof(ChairCleaningLevel).GetField("_brushPixels",fields).GetValue(level);
                    var chairSurface=(bool[])typeof(ChairCleaningLevel).GetField("_surface",fields).GetValue(level);
                    for(int i=0;i<sandMask.Length;i++)if(chairSurface[i])Require(sandMask[i].a>.99f,"Vacuum must remove sand across the entire chair, cell "+i);
                }
                if(step==6)
                {
                    var fields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                    var soap=(Color[])typeof(ChairCleaningLevel).GetField("_pixels",fields).GetValue(level);
                    var swirl=(Color[])typeof(ChairCleaningLevel).GetField("_brushPixels",fields).GetValue(level);
                    int retained=0,agitated=0;for(int i=0;i<soap.Length;i++){if(soap[i].g>.6f)retained++;if(swirl[i].r>.1f)agitated++;}
                    Require(retained>500 && agitated>500,"Brushing must swirl and retain soap, not erase it.");
                }
                if(step==4 || step==5)
                {
                    var fields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                    var mask=(Color[])typeof(ChairCleaningLevel).GetField("_pixels",fields).GetValue(level);int affected=0;
                    foreach(var p in mask)if(step==4?p.b>.9f:p.g>.9f)affected++;
                    Require(affected>200,"Water/foam must persist on chair surface, not just cursor VFX.");
                }
                level.AdvanceSimulation(.8f);Require(level.CurrentStep==step+1,"Step did not advance "+(step+1));
                log.AppendLine("PASS "+(step+1).ToString("00")+": "+ChairCleaningLevel.Steps[step].title);
            }
            Require(!level.BeginPatchDrag(),"Cannot patch before glue.");
            level.SelectTool(1);Require(!level.BeginPatchDrag(),"Patch tool locked until glue complete.");
            level.SelectTool(0);Sweep(level);Require(level.RepairPhase==1,"Glue coverage unlocks patch.");
            Require(!level.IsComplete,"Glue alone cannot complete the level.");
            level.SelectTool(1);Require(level.BeginPatchDrag(),"Patch can now be dragged.");
            Require(!level.PlacePatchUV(Vector2.zero),"Incorrect patch placement rejected.");
            Require(level.BeginPatchDrag(),"Patch can be retried.");Require(level.PlacePatchUV(new Vector2(.49f,.49f)),"Patch placed over tear.");
            Require(level.RepairPhase==2 && !level.IsComplete,"Patch must be smoothed before completion.");
            level.SelectTool(2);Sweep(level);level.AdvanceSimulation(.8f);
            Require(level.IsComplete && level.CurrentStep==13,"All thirteen steps must finish.");
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var material=(Material)typeof(ChairCleaningLevel).GetField("_material",flags).GetValue(level);
            Require(material.GetFloat("_Repair")==1,"Final chair must have no remaining tear.");
            var pixels=(Color[])typeof(ChairCleaningLevel).GetField("_pixels",flags).GetValue(level);
            var surface=(bool[])typeof(ChairCleaningLevel).GetField("_surface",flags).GetValue(level);
            for(int i=0;i<pixels.Length;i++)if(surface[i])Require(pixels[i].r>.99f && pixels[i].g<.01f && pixels[i].b<.01f,"Final surface clean, foam-free, dry at cell "+i);
            for(int i=0;i<pixels.Length;i++)Require(pixels[i].r>.99f,"Detached seaweed contour must also be removed at cell "+i);
            log.AppendLine("PASS 13: glue -> correct leather placement -> smooth; clean, dry and fully repaired.");
            if(restartAfter)
            {
                level.RestartLevel();Require(level.CurrentStep==0 && level.RescuedCount==0 && !level.IsComplete,"Replay resets all state.");
                Require(material.GetFloat("_Repair")==0,"Replay restores the original tear.");
                Require(basket.Count==0,"Replay empties the basket and returns creatures to the chair.");
                log.AppendLine("PASS replay: animals, surface masks, tools and repair reset.");
            }
            string report=log.ToString();System.IO.File.WriteAllText("Assets/MAI_ROOM BÃI BIỂN/ChairLevel/Review/PlaythroughValidation.txt",report);
            return report;
        }
    }
}
