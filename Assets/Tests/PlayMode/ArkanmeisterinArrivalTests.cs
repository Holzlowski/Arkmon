using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public class ArkanmeisterinArrivalTests
{
    static IEnumerator AdvanceLine()
    {
        yield return new WaitForSeconds(.1f);
        Call(Dialogue,"SkipLine");
        yield return null;
        yield return null;
    }
    static void SetSpace(Keyboard keys,bool held)
    {
        InputSystem.QueueStateEvent(keys,held ? new KeyboardState(Key.Space) : new KeyboardState());
        InputSystem.Update();
    }

    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object State(string n) => T("ArkanmeisterinArrival").GetProperty(n).GetValue(null);
    static object Call(object target,string method,params object[] args) => target.GetType().GetMethod(method).Invoke(target,args);
    static object Dialogue => T("DialogueBoxController").GetField("instance").GetValue(null);
    static IEnumerator Load(bool reset) => (IEnumerator)typeof(CourtyardGardenTaskTests)
        .GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{reset});
    static void Capture(string name) => typeof(GameIntroTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name});
    static Component Gate() => UnityEngine.Object.FindObjectsByType(T("CourtyardEntrance"),FindObjectsSortMode.None).Cast<Component>()
        .Single(c=>c.GetType().GetField("destination").GetValue(c).ToString()=="OuterGate");
    static void MoveTo(Component target, Vector3 offset=default)
    {
        var player=GameObject.FindWithTag("Player");
        var position=target.transform.position+Vector3.up+offset;
        player.transform.position=position;
        player.GetComponent<Rigidbody>().position=position;
        Physics.SyncTransforms();
    }

    [UnityTest] public IEnumerator GateStagesArrivalThenOffersGreetingAndKeepsItAcrossReload()
    {
        yield return Load(true);
        Assert.That(GameObject.Find("Arkanmeisterin carriage"),Is.Null);
        var gate=Gate();
        MoveTo(gate);yield return null;
        var focus=GameObject.FindWithTag("Player").GetComponent(T("PlayerInteractionFocus"));
        Assert.That(Call(focus,"Refresh"),Is.EqualTo(gate));
        Call(gate,"InteractWithPlayer");
        Assert.That(State("IsPresenting"),Is.True);
        Assert.That(Call(focus,"Refresh"),Is.Null);
        var coach=GameObject.Find("Arkanmeisterin carriage").transform;
        var door=coach.Find("Carriage door hinge");
        var horse=coach.Find("Carriage horse/Carriage horse illustration").GetComponent<SpriteRenderer>();
        var driver=coach.Find("Coachman/Coachman illustration").GetComponent<SpriteRenderer>();
        Assert.That(horse.sprite.name,Is.EqualTo("Carriage Draft Horse"));
        Assert.That(driver.sprite.name,Is.EqualTo("Carriage Coachman"));
        var reins=coach.Find("Coachman's reins").GetComponent<LineRenderer>();
        Assert.That(Quaternion.Angle(door.localRotation,Quaternion.identity),Is.LessThan(.1f));
        var arrival=GameObject.Find("Arkanmeisterin").GetComponent(T("ArkanmeisterinArrival"));
        var shotProperty=arrival.GetType().GetProperty("CurrentShot",BindingFlags.Instance|BindingFlags.NonPublic);
        var passenger=coach.Find("Hood behind carriage window").GetComponent<SpriteRenderer>();
        var exitFigure=arrival.GetComponentInChildren<SpriteRenderer>();
        var canvas=GameObject.Find("Demo objective")?.GetComponent<Canvas>();
        var order=new List<string>();
        var captured=new HashSet<string>();
        var soundType=T("SoundManager");
        var sound=soundType.GetField("instance").GetValue(null);
        bool observedReveal=false;
        bool observedTitleEntrance=false;
        var revealCameraPosition=Vector3.zero;
        float guardAnnouncementTime=-1;
        bool heldDuringAnnouncement=false;
        var reactionCameraPosition=Vector3.zero;
        var hintCanvas=GameObject.Find("Nearby interaction").GetComponent<Canvas>();
        var hintRing=GameObject.Find("Interaction ground halo").GetComponent<LineRenderer>();
        var elian=GameObject.FindWithTag("Player").transform;
        var controller=elian.GetComponent(T("PlayerController"));
        bool observedReaction=false;
        bool observedApproach=false;
        bool heldAfterTitle=false;
        float previousMusicDucking=(float)Call(sound,"get_MusicDucking");
        float deadline=Time.realtimeSinceStartup+58;
        while((bool)State("IsPresenting") && Time.realtimeSinceStartup<deadline)
        {
            string shot=shotProperty.GetValue(arrival).ToString();
            if(shot!="None" && (order.Count==0 || order[order.Count-1]!=shot))order.Add(shot);
            var title=shot=="Reveal" ? GameObject.Find("Arkanmeisterin arrival title") : null;
            var titleGroup=title!=null ? title.GetComponentInChildren<CanvasGroup>() : null;
            if(shot=="Reveal")
            {
                if(!observedReveal)
                {
                    observedReveal=true;
                    revealCameraPosition=Camera.main.transform.position;
                }
                Assert.That(Camera.main.fieldOfView,Is.EqualTo(27f).Within(.01f));
                Assert.That(Vector3.Distance(Camera.main.transform.position,revealCameraPosition),Is.LessThan(.001f));
                if(!observedTitleEntrance && titleGroup!=null && titleGroup.alpha>.01f)
                {
                    observedTitleEntrance=true;
                    Assert.That(Call(sound,"get_LastEffectName"),Is.EqualTo("IntroFoley/arkanmeisterin-reveal"));
                }
                heldAfterTitle |= title!=null && !title.GetComponent<Canvas>().enabled;
            }
            if(shot=="Reaction")
            {
                if(!observedReaction)
                {
                    observedReaction=true;
                    reactionCameraPosition=Camera.main.transform.position;
                }
                Assert.That(Camera.main.fieldOfView,Is.EqualTo(34f).Within(.01f));
                Assert.That(Vector3.Distance(Camera.main.transform.position,reactionCameraPosition),Is.LessThan(.001f));
                Assert.That(Call(Dialogue,"get_IsOpen"),Is.False);
            }
            if(shot=="Guard" && (string)Call(Dialogue,"get_ActiveSpeaker")=="Burgwache")
            {
                if(guardAnnouncementTime<0) guardAnnouncementTime=Time.time;
                Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
                Assert.That(Quaternion.Angle(door.localRotation,Quaternion.identity),Is.LessThan(.1f));
                Assert.That(passenger.enabled,Is.True);
                Assert.That(arrival.GetComponent<AudioSource>(),Is.Null);
                if(Time.time-guardAnnouncementTime>.75f)
                {
                    heldDuringAnnouncement=true;
                    Call(Dialogue,"CloseDialogue");
                }
            }
            if(shot=="Approach" || shot=="Greeting")
            {
                Assert.That(canvas.enabled,Is.False);
                Assert.That(hintCanvas.enabled,Is.False);
                Assert.That(hintRing.enabled,Is.False);
                Assert.That(Call(focus,"Refresh"),Is.Null);
                Assert.That(Camera.main.fieldOfView,Is.EqualTo(34f).Within(.01f));
                if(shot=="Greeting") break;

                Assert.That(Call(controller,"get_IsScriptedMoving"),Is.True);
                if(!observedApproach && Vector3.Distance(Camera.main.transform.position,reactionCameraPosition)>.06f)
                {
                    observedApproach=true;
                    Capture("10b-elian-approaches");
                }
            }
            switch(shot)
            {
                case "Road" when coach.position.x<74.5f && captured.Add(shot):
                    Assert.That(Camera.main.transform.position.y,Is.GreaterThan(10));
                    Assert.That(coach.position.x,Is.GreaterThan(64));
                    Assert.That(Quaternion.Angle(horse.transform.rotation,coach.rotation),Is.LessThan(.1f));
                    Assert.That(Quaternion.Angle(driver.transform.rotation,coach.rotation),Is.LessThan(.1f));
                    Assert.That(GameObject.FindWithTag("Player").transform.position.z,Is.LessThan(-3));
                    Assert.That(canvas==null || !canvas.enabled,Is.True);
                    Assert.That((float)Call(sound,"get_MusicDucking"),Is.GreaterThan(0).And.LessThan(previousMusicDucking));
                    Assert.That(arrival.GetComponent<AudioSource>(),Is.Null);
                    Capture("01-ueber-dem-burgtor");
                    break;
                case "Wheel" when coach.position.x<62.8f && captured.Add(shot):
                    var wheel=coach.Cast<Transform>().Single(child=>child.name=="Carriage wheel" && child.localPosition.x>0 && child.localPosition.z<0);
                    var center=Camera.main.WorldToViewportPoint(wheel.position);
                    Assert.That(center.x,Is.EqualTo(.5f).Within(.02f));
                    Assert.That(center.y,Is.EqualTo(.5f).Within(.02f));
                    Assert.That((float)Call(sound,"get_MusicDucking"),Is.Zero);
                    Capture("02-wagenrad");
                    break;
                case "Side" when coach.position.x<55.5f && captured.Add(shot):
                    Assert.That(passenger.enabled,Is.True);
                    Assert.That(driver.bounds.Contains(reins.GetPosition(0)),Is.True);
                    Assert.That(horse.bounds.Contains(reins.GetPosition(2)),Is.True);
                    Capture("03-seitliche-mitfahrt");
                    break;
                case "Gate" when coach.position.x<44.4f && captured.Add(shot):
                    Capture("04-durch-das-tor");
                    break;
                case "Coachman" when coach.position.x<36.5f && captured.Add(shot):
                    Assert.That(GameObject.Find("Coachman arrival subtitle"),Is.Null);
                    Assert.That(Call(sound,"get_LastEffectName"),Is.Not.EqualTo("IntroFoley/coachman-halt"));
                    Capture("05-kutscher");
                    break;
                case "Guard" when GameObject.Find("Courtyard Guard").transform.position.x>33.5f && captured.Add(shot):
                    Assert.That(GameObject.Find("Guard sweat drops"),Is.Not.Null);
                    Capture("06-wache");
                    break;
                case "Step" when captured.Add(shot):
                    Assert.That(heldDuringAnnouncement,Is.True);
                    Assert.That(Call(Dialogue,"get_IsOpen"),Is.False);
                    Assert.That(Quaternion.Angle(door.localRotation,Quaternion.identity),Is.GreaterThan(90));
                    Assert.That(passenger.enabled,Is.False);
                    Assert.That(exitFigure.enabled,Is.True);
                    var guardAtDoor=GameObject.Find("Courtyard Guard");
                    var guardState=guardAtDoor.GetComponent(T("CourtyardGuard"));
                    Assert.That((bool)Call(guardState,"get_FacingLeft"),Is.False);
                    Assert.That(guardAtDoor.transform.position.x,Is.LessThan(exitFigure.transform.position.x-1.2f));
                    Assert.That(exitFigure.transform.position.y-coach.position.y,Is.EqualTo(.53f).Within(.02f));
                    var theme=arrival.GetComponent<AudioSource>();
                    Assert.That(theme.clip.name,Is.EqualTo("arkanmeisterin-theme"));
                    Assert.That(theme.volume,Is.GreaterThan(0).And.LessThan((float)soundType.GetField("musicVolume").GetValue(sound)*.65f));
                    Capture("07-stiefel-trittbrett");
                    break;
                case "Landing" when captured.Add(shot):
                    Assert.That(exitFigure.transform.position,Is.EqualTo(arrival.transform.position));
                    Capture("08-stiefel-am-boden");
                    break;
                case "Reveal" when titleGroup!=null && titleGroup.alpha>.95f && captured.Add(shot):
                    Assert.That(State("Arrived"),Is.True);
                    var guardParts=GameObject.Find("Guard paper walking rig").GetComponentsInChildren<SpriteRenderer>();
                    float guardHeight=guardParts.Max(part=>part.bounds.max.y)-guardParts.Min(part=>part.bounds.min.y);
                    Assert.That(guardHeight/exitFigure.bounds.size.y,Is.InRange(.96f,1.08f),"The two standing adults should be close in height.");
                    Assert.That(exitFigure.sprite.rect.xMin,Is.Zero);
                    Assert.That(Call(sound,"get_LastEffectName"),Is.EqualTo("IntroFoley/arkanmeisterin-reveal"));

                    var lettering=(RectTransform)title.transform.Find("Arrival title lettering");
                    Assert.That(lettering.anchorMin.x,Is.EqualTo(.65f).Within(.01f));
                    var labels=title.GetComponentsInChildren<TMPro.TMP_Text>().Select(label=>label.text);
                    Assert.That(labels,Is.EquivalentTo(new[]{"Arkanmeisterin","Gekommen, um Elian zu prüfen."}));
                    Capture("09-arkanmeisterin");
                    break;
                case "Reaction" when captured.Add(shot):
                    var expression=GameObject.Find("Elian arrival expression").GetComponent<SpriteRenderer>();
                    Assert.That(expression.enabled,Is.True);
                    Vector3 facing=expression.transform.TransformVector(Vector3.right);
                    Assert.That(Vector3.Dot(facing,arrival.transform.position-expression.transform.position),Is.GreaterThan(0));
                    Capture("10-elian-reaktion");
                    break;
            }
            yield return null;
        }
        Assert.That(order,Is.EqualTo(new[]{"Rise","Road","Wheel","Side","Gate","Coachman","Guard","Step","Landing","Reveal","Reaction","Approach","Greeting"}));
        Assert.That(captured.Count,Is.EqualTo(10));
        var continuingTheme=arrival.GetComponent<AudioSource>();
        Assert.That(continuingTheme.loop,Is.True);
        float quietVolume=(float)soundType.GetField("musicVolume").GetValue(sound)*.42f;
        Assert.That(continuingTheme.volume,Is.EqualTo(quietVolume).Within(.001f));
        Assert.That((float)Call(sound,"get_MusicDucking"),Is.Zero);
        Assert.That(GameObject.Find("Elian arrival expression").GetComponent<SpriteRenderer>().enabled,Is.False);
        Assert.That(observedReveal,Is.True);
        Assert.That(observedTitleEntrance,Is.True);
        Assert.That(heldAfterTitle,Is.True);
        Assert.That(observedApproach,Is.True);
        Assert.That(canvas.enabled,Is.False);
        Assert.That(State("IsPresenting"),Is.True);
        Assert.That(State("Arrived"),Is.True);
        Assert.That(Quaternion.Angle(door.localRotation,Quaternion.identity),Is.GreaterThan(90f));
        Assert.That(heldDuringAnnouncement,Is.True);
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        Assert.That(Call(controller,"get_IsScriptedMoving"),Is.False);
        var mentor=arrival;
        var figure=mentor.GetComponentInChildren<SpriteRenderer>();
        var raised=figure.sprite;
        var separation=elian.position-mentor.transform.position;
        Assert.That(separation.x,Is.EqualTo(1.55f).Within(.06f));
        Assert.That(separation.z,Is.EqualTo(-.15f).Within(.06f));
        var greetingCameraPosition=Camera.main.transform.position;
        var maskedPortrait=(Sprite)Call(Dialogue,"get_ActivePortrait");
        Assert.That(maskedPortrait,Is.Not.Null);
        Assert.That(State("Greeted"),Is.False);
        yield return new WaitForSeconds(1.15f);
        Capture("meisterin-hood-up");
        yield return AdvanceLine();
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Elian"));
        yield return AdvanceLine();
        Assert.That(Call(Dialogue,"get_ActivePortrait"),Is.EqualTo(maskedPortrait));
        Assert.That(figure.sprite,Is.EqualTo(raised));
        Assert.That(State("Greeted"),Is.False);
        Assert.That(Camera.main.fieldOfView,Is.EqualTo(34).Within(.01f));
        Assert.That(Vector3.Distance(Camera.main.transform.position,greetingCameraPosition),Is.LessThan(.001f));
        Assert.That(canvas==null || !canvas.enabled,Is.True);
        var elianFace=Camera.main.WorldToViewportPoint(GameObject.FindWithTag("Player").transform.position+Vector3.up*.85f);
        var mentorFace=Camera.main.WorldToViewportPoint(mentor.transform.position+Vector3.up*2);
        foreach(var face in new[]{elianFace,mentorFace})
        {
            Assert.That(face.x,Is.InRange(.1f,.9f));
            Assert.That(face.y,Is.InRange(.40f,.95f));
        }
        Capture("11-kapuze-oben");
        yield return AdvanceLine();
        Assert.That(figure.sprite,Is.Not.EqualTo(raised));
        Assert.That(State("Greeted"),Is.False);
        Call(Dialogue,"CloseDialogue");
        yield return null;
        yield return null;
        Assert.That(figure.sprite,Is.EqualTo(raised));
        Assert.That(State("IsPresenting"),Is.False);
        Assert.That(GameObject.Find("Arkanmeisterin arrival title"),Is.Null);
        Assert.That(Camera.main.fieldOfView,Is.GreaterThan(34));
        Assert.That(canvas.enabled,Is.True);
        Assert.That(Call(focus,"Refresh"),Is.EqualTo(mentor));
        Assert.That(mentor.GetComponent<AudioSource>(),Is.SameAs(continuingTheme));

        Call(mentor,"InteractWithPlayer");
        yield return AdvanceLine();
        yield return AdvanceLine();
        var stand = figure.transform.position;
        float started = Time.time;
        yield return AdvanceLine();
        var poses = new List<Sprite>();
        while (!(bool)State("Greeted") && Time.time < started + 4)
        {
            Assert.That(Call(Dialogue,"get_ActivePortrait"),Is.EqualTo(maskedPortrait));
            Assert.That(Camera.main.fieldOfView,Is.EqualTo(34).Within(.01f));
            Assert.That(figure.transform.position,Is.EqualTo(stand));
            Assert.That(figure.sprite.bounds.size.y,Is.EqualTo(raised.bounds.size.y).Within(.001f));
            Assert.That(mentor.GetComponent<AudioSource>(),Is.SameAs(continuingTheme));
            Assert.That(continuingTheme.volume,Is.EqualTo(quietVolume).Within(.001f));
            Call(Dialogue,"SkipLine");
            if (!poses.Contains(figure.sprite))
            {
                poses.Add(figure.sprite);
                if (poses.Count == 5) Capture("12-am-kapuzenrand");
                if (poses.Count == 8) Capture("13-kapuze-halber-zug");
            }
            yield return null;
        }
        Assert.That(poses.Select(pose => pose.name),Is.EqualTo(Enumerable.Range(1,10).Select(i => $"Hood pull {i:00}")));
        Assert.That(Time.time-started,Is.GreaterThanOrEqualTo(1.8f));
        yield return null;
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        Assert.That(Call(Dialogue,"get_ActivePortrait"),Is.Not.EqualTo(maskedPortrait));
        Assert.That(figure.sprite,Is.Not.EqualTo(raised));
        Assert.That(State("Greeted"),Is.True);
        yield return new WaitForSeconds(.12f);
        Assert.That(mentor.GetComponent<AudioSource>(),Is.SameAs(continuingTheme));
        Assert.That(continuingTheme.volume,Is.EqualTo(quietVolume).Within(.001f));
        Capture("14-kapuze-unten");
        Capture("meisterin-hood-down");
        Call(Dialogue,"CloseDialogue");
        Assert.That(State("ObjectiveText"),Is.Null);
        Assert.That(Camera.main.fieldOfView,Is.GreaterThan(34));
        Assert.That(canvas==null || canvas.enabled,Is.True);
        yield return new WaitForSecondsRealtime(1.55f);
        Assert.That(mentor.GetComponent<AudioSource>(),Is.Null);
        Assert.That((float)Call(sound,"get_MusicDucking"),Is.EqualTo(previousMusicDucking));

        var trialType=T("CourtyardVesselTrial");
        var vessel=trialType.GetProperty("HasVessel");
        var completed=trialType.GetProperty("Completed");
        var chargeProperty=trialType.GetProperty("Charge",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(vessel.GetValue(null),Is.False);
        var gateGreeting = mentor.transform.position;
        Camera.main.aspect = 16f/9f;
        Call(mentor,"InteractWithPlayer");
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        Assert.That(Call(controller,"get_IsScriptedMoving"),Is.False);
        var elianPaper=elian.GetComponent(T("PaperCharacterVisual"));
        var elianSprite=(SpriteRenderer)elianPaper.GetType().GetField("spriteRenderer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(elianPaper);
        Assert.That(elianSprite.flipX,Is.EqualTo(Vector3.Dot(mentor.transform.position-elian.position,Camera.main.transform.right)<0));
        yield return new WaitForSeconds(.75f);
        Assert.That(Call(controller,"get_IsScriptedMoving"),Is.True);
        yield return AdvanceLine();
        yield return AdvanceLine();
        float walkDeadline=Time.time+8;
        while ((string)Call(Dialogue,"get_ActiveSpeaker")!="Elian" && Time.time<walkDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Elian"));
        Assert.That(mentor.transform.position.x,Is.LessThan(gateGreeting.x-4));
        Assert.That(mentor.transform.position.y,Is.GreaterThan(-1));
        Assert.That(Call(controller,"get_IsScriptedMoving"),Is.True);
        Capture("15-hof-gespraech-im-gehen");
        yield return AdvanceLine();
        yield return AdvanceLine();
        while ((string)Call(Dialogue,"get_ActiveSpeaker")!="Arkanmeisterin" && Time.time<walkDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        yield return AdvanceLine();
        yield return AdvanceLine();
        float offerDeadline=Time.time+8;
        while ((figure.sprite.name!="Vessel offered" || !(bool)Call(Dialogue,"get_IsOpen"))
            && Time.time<offerDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        Assert.That(Call(controller,"get_IsScriptedMoving"),Is.False);
        Assert.That(mentor.transform.position.x,Is.LessThan(gateGreeting.x-20));
        Assert.That(Vector3.Distance(mentor.transform.position,elian.position),Is.LessThan(2.3f));
        Assert.That(State("IsPresenting"),Is.True);
        var trial=mentor.GetComponent(trialType);
        Assert.That(trial,Is.Not.Null);
        float Charge() => (float)chargeProperty.GetValue(trial);
        Assert.That(Charge(),Is.Zero);
        Assert.That(figure.sprite.name,Is.EqualTo("Vessel offered"));
        Capture("16-gefaess-uebergabe");
        var residents=new[]{"CourtyardGardenerVisual","CourtyardStablekeeperVisual","CourtyardCarpenterVisual"}
            .Select(type=>(Component)UnityEngine.Object.FindFirstObjectByType(T(type))).ToArray();
        var homes=residents.Select(resident=>resident.transform.position).ToArray();
        Call(Dialogue,"CloseDialogue");
        float concentrationDeadline=Time.time+12;
        while (GameObject.Find("Vessel concentration")==null && Time.time<concentrationDeadline) yield return null;
        Assert.That(vessel.GetValue(null),Is.True);
        Assert.That(GameObject.Find("Vessel concentration"),Is.Not.Null);
        foreach (var resident in residents)
        {
            Assert.That(Vector3.Distance(resident.transform.position,mentor.transform.position),Is.LessThan(4.5f));
            var head=Camera.main.WorldToViewportPoint(resident.transform.position+Vector3.up*1.65f);
            Assert.That(head.x,Is.InRange(.08f,.92f));
            Assert.That(head.y,Is.InRange(.20f,.90f));
        }
        Assert.That(Call(focus,"Refresh"),Is.Null);
        Assert.That(completed.GetValue(null),Is.False);
        var darkness=GameObject.Find("Vessel trial lighting").GetComponent<SpriteRenderer>();
        var arcane=GameObject.Find("Arcane colors").GetComponent<SpriteRenderer>();
        var bar=GameObject.Find("Vessel charge fill").GetComponent<RectTransform>();
        bool HasPose(string name) => elian.GetComponentsInChildren<SpriteRenderer>().Any(part=>part.sprite?.name==name);
        yield return new WaitForSeconds(.45f);
        Assert.That(Charge(),Is.Zero);
        Assert.That(bar.anchorMax.x,Is.Zero);
        Assert.That(darkness.color.a,Is.EqualTo(0).Within(.03f));
        Assert.That(arcane.sharedMaterial.GetFloat("_Strength"),Is.Zero.Within(.03f));
        Capture("17-gefaess-konzentration");

        var input=new InputTestFixture();
        input.Setup();
        try
        {
            var keys=InputSystem.AddDevice<Keyboard>();
            keys.MakeCurrent();
            SetSpace(keys,true);
            float chargeDeadline=Time.time+6.5f;
            while (Charge()<.46f && Time.time<chargeDeadline) yield return null;
            Assert.That(Charge(),Is.InRange(.46f,.52f));
            Assert.That(bar.anchorMax.x,Is.EqualTo(Charge()).Within(.01f));
            Assert.That(HasPose("Elian straining"),Is.True);
            Assert.That(darkness.color.a,Is.GreaterThan(.30f));
            float brighterArcane=arcane.sharedMaterial.GetFloat("_Strength");
            Capture("18-gefaess-angestrengt");

            SetSpace(keys,false);
            yield return new WaitForSeconds(2.9f);
            Assert.That(Charge(),Is.InRange(.28f,.355f),"Releasing Space should slowly drain a partially charged vessel.");
            Assert.That(bar.anchorMax.x,Is.EqualTo(Charge()).Within(.01f));
            Assert.That(HasPose("Elian concentrating"),Is.True);
            Assert.That(arcane.sharedMaterial.GetFloat("_Strength"),Is.LessThan(brighterArcane));
            Assert.That(completed.GetValue(null),Is.False);

            SetSpace(keys,true);
            chargeDeadline=Time.time+8.5f;
            while (Charge()<.93f && Time.time<chargeDeadline) yield return null;
            Assert.That(Charge(),Is.GreaterThanOrEqualTo(.93f));
            Assert.That(darkness.color.a,Is.GreaterThan(.75f));
            Assert.That(arcane.sharedMaterial.GetFloat("_Strength"),Is.GreaterThan(.94f));
            Assert.That(HasPose("Elian maximum effort"),Is.True);
            Capture("19-gefaess-hof-dunkel");
            while (!(bool)completed.GetValue(null) && Time.time<chargeDeadline+2) yield return null;
            SetSpace(keys,false);
        }
        finally { input.TearDown(); }
        yield return new WaitForSeconds(.24f);
        Assert.That(completed.GetValue(null),Is.True);
        Assert.That(GameObject.Find("Vessel magic sparks").GetComponent<ParticleSystem>().particleCount,Is.GreaterThan(0));
        var raisedVessel=elian.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.sprite?.name=="Elian proud vessel lift");
        var glow=GameObject.Find("Vessel light").GetComponent<Light>();
        Assert.That(glow.transform.position.y,Is.GreaterThan(raisedVessel.bounds.center.y+.2f));
        Capture("20-gefaess-antwort");
        float congratulationDeadline=Time.time+5;
        while (!(bool)Call(Dialogue,"get_IsOpen") && Time.time<congratulationDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_ActiveSpeaker"),Is.EqualTo("Arkanmeisterin"));
        Assert.That(darkness.color.a,Is.Zero.Within(.03f));
        Assert.That(glow.intensity,Is.GreaterThan(2));
        Capture("21-gefaess-glueckwunsch");
        Call(Dialogue,"CloseDialogue");
        yield return null;
        yield return null;
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
        Assert.That(State("IsPresenting"),Is.True);
        Assert.That(Call(focus,"Refresh"),Is.Null);
        var starterType = T("CourtyardStarterSummoning");
        var summoned = starterType.GetProperty("Completed");
        Assert.That(summoned.GetValue(null),Is.False);
        var starterTriggers = new[] { "Courtyard Kafla", "Courtyard Panfrie" }
            .Select(name=>GameObject.Find(name).GetComponent<SphereCollider>()).ToArray();
        Assert.That(starterTriggers.Any(trigger=>trigger.enabled),Is.False);
        Call(Dialogue,"CloseDialogue");
        float spellDeadline = Time.time+4f;
        while (GameObject.Find("Courtyard starter summoning")==null && Time.time<spellDeadline) yield return null;
        var spell = GameObject.Find("Courtyard starter summoning");
        Assert.That(spell,Is.Not.Null);
        var spellSound = spell.GetComponent<AudioSource>();
        Assert.That(spellSound.clip.name,Is.EqualTo("courtyard-summoning"));
        Assert.That(spellSound.clip.length,Is.EqualTo(8).Within(.02f));
        var actors = new[] { "Kafla","Panfrie","Liora","Rauk" }
            .Select(name=>spell.transform.Find("Summoned "+name)).ToArray();
        Assert.That(actors.All(actor=>actor!=null),Is.True);
        Assert.That(actors.All(actor=>!actor.GetComponent<SpriteRenderer>().enabled),Is.True);
        yield return new WaitForSeconds(1.9f);
        Assert.That(figure.sprite.name,Is.EqualTo("Vessel raised"));
        Assert.That(spell.transform.Find("Kafla orb core").GetComponent<SpriteRenderer>().enabled,Is.True);
        Capture("22-beschwoerung-flug");
        spellDeadline = Time.time+8f;
        while (!(bool)Call(Dialogue,"get_IsOpen") && Time.time<spellDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
        Assert.That(actors.All(actor=>actor.GetComponent<SpriteRenderer>().enabled),Is.True);
        foreach (var actor in actors)
            Assert.That(Camera.main.WorldToViewportPoint(actor.position).y,Is.GreaterThan(.36f),
                actor.name+" must stay above the dialogue panel.");
        Capture("23-beschwoerung-starter");
        var marks = actors.Select(actor=>actor.position).ToArray();
        Call(Dialogue,"CloseDialogue");
        yield return new WaitForSeconds(1f);
        Assert.That(actors.Select((actor,i)=>Vector3.Distance(actor.position,marks[i])).Any(distance=>distance>.75f),Is.True);
        spellDeadline = Time.time+4f;
        while (!(bool)Call(Dialogue,"get_IsOpen") && Time.time<spellDeadline) yield return null;
        Assert.That(Call(Dialogue,"get_IsOpen"),Is.True);
        Assert.That(actors.All(actor=>!actor.GetComponent<SpriteRenderer>().enabled),Is.True);
        Call(Dialogue,"CloseDialogue");
        yield return null;
        yield return null;
        Assert.That(summoned.GetValue(null),Is.True);
        Assert.That(starterTriggers.All(trigger=>trigger.enabled),Is.True);
        Assert.That(GameObject.Find("Courtyard starter summoning"),Is.Null);
        Assert.That(State("IsPresenting"),Is.False);
        Assert.That(GameObject.Find("Vessel concentration"),Is.Null);
        Assert.That(GameObject.Find("Vessel trial lighting"),Is.Null);
        for (int i=0;i<residents.Length;i++)
            Assert.That(residents[i].transform.position,Is.EqualTo(homes[i]));
        Assert.That(canvas.enabled,Is.True);

        yield return Load(false);
        Assert.That(State("Greeted"),Is.True);
        Assert.That(vessel.GetValue(null),Is.True);
        Assert.That(completed.GetValue(null),Is.True);
        Assert.That(summoned.GetValue(null),Is.True);
        Assert.That(State("IsPresenting"),Is.False);
        Assert.That(GameObject.Find("Arkanmeisterin carriage"),Is.Not.Null);
        mentor=GameObject.Find("Arkanmeisterin").GetComponent(T("ArkanmeisterinArrival"));
        figure=mentor.GetComponentInChildren<SpriteRenderer>();
        Assert.That(figure.sprite.rect.xMin,Is.GreaterThan(0),"The revealed pose survives returning to the courtyard.");
        gate=Gate();MoveTo(gate);Call(gate,"InteractWithPlayer");
        Assert.That(State("IsPresenting"),Is.False);
        Assert.That(UnityEngine.Object.FindObjectsByType(T("ArrivalCarriage"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        LogAssert.NoUnexpectedReceived();
    }
}
