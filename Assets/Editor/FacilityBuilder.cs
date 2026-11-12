using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private const string ScenePath = "Assets/Scenes/PolarityFacility.unity";
        private const string MassScenePath = "Assets/Scenes/MassTransfer.unity";
        private const string MenuScenePath = "Assets/Scenes/LevelMenu.unity";
        private static Material deck, dark, steel, pale, stripe, red, blue, idle, ready, black, glow;
        private static Transform geometry, detail, background;

        [MenuItem("Tools/Polarity Facility/Rebuild Demo Scene")]
        public static void CreateScene() => CreateScene(false);

        [MenuItem("Tools/Polarity Facility/Rebuild Mass Transfer Scene")]
        public static void CreateMassTransferScene() => CreateScene(true);

        [MenuItem("Tools/Polarity Facility/Rebuild Level Menu")]
        public static void CreateLevelMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level Menu", typeof(Camera), typeof(AudioListener), typeof(LevelNavigation));
            var camera = root.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 35, 53, 255);
            camera.cullingMask = 0;
            SavePlayableScene(scene, MenuScenePath);

            var scenes = EditorBuildSettings.scenes;
            int menuIndex = Array.FindIndex(scenes, entry => entry.path == MenuScenePath);
            if (menuIndex < 0) throw new InvalidOperationException("Level menu was not saved");
            Array.Copy(scenes, 0, scenes, 1, menuIndex);
            scenes[0] = new EditorBuildSettingsScene(MenuScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }

        private static void CreateScene(bool massChallenge)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Art/Materials");
            AssetDatabase.Refresh();
            BuildMaterials();
            geometry = new GameObject("01  Suspended platforms").transform;
            detail = new GameObject("02  Machine details").transform;
            background = new GameObject("03  Lower facility").transform;

            Platform("Intake", -12.5f, -3.35f, -2.85f, 2.85f, 0, 4.5f);
            Platform("Transfer", -1.3f, massChallenge ? 4.85f : 6.05f, -2.65f, 3.35f, -.62f, 5.7f);
            if (!massChallenge)
            {
                Platform("Magnetic catwalk", 6.058f, 8.992f, 1.12f, 4.05f, -.62f, 2.8f, 5.97f, 9.05f);
                Platform("Pair terminal", 9.0f, 15.65f, .08f, 5.95f, -.62f, 5.0f);
            }
            Transform bridge = Box("Lift bridge", new Vector3(-2.32f,-.85f,0), new Vector3(2.02f,.46f,2.38f),
                steel, geometry, true);
            // Keep the overlapping collision footprint, but leave a seam between the visible decks.
            bridge.GetComponent<BoxCollider>().size = new Vector3(2.13f / 2.02f, 1, 1);
            LocalBox(bridge,"Lift bridge luminous insert",new Vector3(0,.502f,0),new Vector3(.89f,.028f,.8f),pale);
            LocalBox(bridge,"Guide line",new Vector3(0,.526f,0),new Vector3(.93f,.029f,.10f),ready);

            Transform anchor = Mark("Anchor recess center", new Vector3(-4.85f,0,0));
            Socket(anchor.position, .62f, stripe, "Anchor recess");
            Box("Anchor rear jaw", new Vector3(-5.26f,.19f,.75f),new Vector3(.58f,.38f,.27f),steel,detail,false);
            Box("Anchor front jaw", new Vector3(-5.26f,.19f,-.75f),new Vector3(.58f,.38f,.27f),steel,detail,false);
            Box("Throw lane",new Vector3(-4.04f,.016f,0),new Vector3(1.39f,.032f,.075f),stripe,detail,false);
            Box("Break edge beam",new Vector3(-3.39f,-.24f,0),new Vector3(.12f,.24f,5.60f),stripe,detail,false);
            Transform farSwitch = Mark("Transfer contact",new Vector3(.66f,-.62f,0));
            Renderer switchLamp = Socket(farSwitch.position, .78f, idle, "Receiver plate");
            Box("Receiver cue",new Vector3(-.62f,-.603f,0),new Vector3(1.08f,.025f,.06f),pale,detail,false);

            Frame(new Vector3(-10.99f,0,2.43f), 2.65f,2.8f, false);
            Frame(new Vector3(3.25f,-.62f,2.93f), 2.7f,3.15f, false);
            Transform rail = null, gate = null, socketLeft = null, socketRight = null;
            Renderer gateLamp = null, l = null, r = null;
            if (!massChallenge)
            {
                rail = Mark("Fixed south pole",new Vector3(6.81f,-.62f,3.83f));
                Box("Magnetic column", rail.position + Vector3.up * .95f, new Vector3(1.16f,1.9f,.47f),dark,detail,true);
                Box("Fixed pole face",rail.position+new Vector3(0,1.02f,-.249f),new Vector3(.9f,1.26f,.06f),steel,detail,false);
                for (int i = 0; i < 4; i++)
                    Box("Pole fins",rail.position+new Vector3(0,.62f + i * .23f,-.289f),new Vector3(.74f,.083f,.09f),
                        ready,detail,false);
                Frame(new Vector3(12.1f,-.62f,3.05f), 4.84f,3.5f,true);
                gate = Box("Shutter",new Vector3(12.1f,.82f,3.05f),new Vector3(.29f,2.92f,4.78f),
                    steel,geometry,true);
                for(int i=0;i<7;i++)
                    LocalBox(gate,"Shutter slat",new Vector3(-.55f,-.43f+i*.138f,0),new Vector3(.24f,.035f,.96f),dark);
                gateLamp = Box("Terminal status",new Vector3(11.72f,2.57f,3.05f),new Vector3(.12f,.2f,1.45f),idle,detail,false).GetComponent<Renderer>();
                socketLeft = Mark("Low socket",new Vector3(10.43f,-.62f,1.83f));
                socketRight = Mark("High socket",new Vector3(10.43f,-.62f,4.27f));
                l = Socket(socketLeft.position,.66f,idle,"Low terminal plate");
                r = Socket(socketRight.position,.66f,idle,"High terminal plate");
            }

            Transform exit = Mark("Pair exit", massChallenge ? new Vector3(3.18f,-.62f,.15f) : new Vector3(13.89f,-.62f,3.05f));
            Socket(exit.position,1.48f,ready,"Exit annulus");
            float backstop = exit.position.x + 1.62f;
            Box("Exit backstop",new Vector3(backstop,.45f,exit.position.z),new Vector3(.27f,2.11f,4.8f),dark,geometry,true);
            for(int i=0;i<5;i++)
                Box("Exit backstop ribs",new Vector3(backstop-.18f,.38f,exit.position.z-2.08f+i*1.04f),new Vector3(.19f,1.72f,.23f),
                    i%2==0 ? ready:steel,detail,false);
            Decorate(massChallenge);

            Vector3 spawnA = massChallenge ? new Vector3(-6.7f,.50f,-.64f) : new Vector3(-10.55f,.50f,-.79f);
            Vector3 spawnB = massChallenge ? new Vector3(-10.15f,.50f,.88f) : new Vector3(-8.93f,.50f,.77f);
            var bodyA = Ball("Body A",spawnA,red,out var shellA,out var haloA,out var trailA);
            var bodyB = Ball("Body B",spawnB,blue,out var shellB,out var haloB,out var trailB);
            var field = Line("Pair field",49,.038f);
            field.positionCount = 37;
            Camera camera = Lighting(massChallenge);
            var root = new GameObject("04  Facility simulation");
            var level = root.AddComponent<FacilityLevel>();
            var so = new SerializedObject(level);
            PutArray(so,"bodies",bodyA,bodyB); PutArray(so,"shells",shellA,shellB);
            PutArray(so,"halos",haloA,haloB); PutArray(so,"trails",trailA,trailB);
            Put(so,"northShell",red); Put(so,"southShell",blue); Put(so,"field",field);
            Put(so,"anchor",anchor); Put(so,"farSwitch",farSwitch); Put(so,"rail",rail);
            Put(so,"socketLeft",socketLeft); Put(so,"socketRight",socketRight); Put(so,"exit",exit);
            Put(so,"bridge",bridge); Put(so,"gate",gate); Put(so,"switchLight",switchLamp);
            Put(so,"socketLightLeft",l); Put(so,"socketLightRight",r); Put(so,"gateLight",gateLamp);
            Put(so,"standbyLight",idle); Put(so,"readyLight",ready); Put(so,"view",camera);
            if (massChallenge) Put(so,"massTransfer", AddMassStation(root, bodyA, bodyB));
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePlayableScene(root.scene, massChallenge ? MassScenePath : ScenePath);
        }

        private static void SavePlayableScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            var scenes = EditorBuildSettings.scenes;
            if (!Array.Exists(scenes, entry => entry.path == path))
            {
                Array.Resize(ref scenes, scenes.Length + 1);
                scenes[scenes.Length - 1] = new EditorBuildSettingsScene(path, true);
                EditorBuildSettings.scenes = scenes;
            }
            AssetDatabase.SaveAssets();
        }

        private static MassTransferStation AddMassStation(GameObject root, Rigidbody bodyA, Rigidbody bodyB)
        {
            Transform near = Mark("Ballast station near pad", new Vector3(-8.7f, 0, -1.55f));
            Transform far = Mark("Ballast station far pad", new Vector3(-8.7f, 0, 1.55f));
            Vector3 center = (near.position + far.position) * .5f;
            Box("Ballast station channel", center + Vector3.up * .019f, new Vector3(.44f,.038f,3.08f), dark, detail, false);
            Renderer conduit = Box("Ballast station link", center + Vector3.up * .04f, new Vector3(.11f,.018f,2.9f),
                idle, detail, false).GetComponent<Renderer>();
            var station = root.AddComponent<MassTransferStation>();
            var so = new SerializedObject(station);
            Put(so,"nearPad",near); Put(so,"farPad",far);
            Put(so,"nearLamp",Socket(near.position,.71f,idle,"Near ballast pad"));
            Put(so,"farLamp",Socket(far.position,.71f,idle,"Far ballast pad"));
            Put(so,"conduitLamp",conduit); Put(so,"idle",idle); Put(so,"live",ready);
            BallastBand(bodyA.transform, 0, 1.07f);
            var ballastA = new GameObject("Transferred ballast");
            ballastA.transform.SetParent(bodyA.transform, false);
            BallastBand(ballastA.transform, -.19f, 1.00f);
            BallastBand(ballastA.transform, .19f, 1.00f);
            GameObject ballastB = BallastBand(bodyB.transform, 0, 1.07f);
            ballastA.SetActive(false);
            Put(so,"ballastA",ballastA); Put(so,"ballastB",ballastB);
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }

        private static GameObject BallastBand(Transform parent, float height, float diameter)
        {
            Transform band = Primitive(PrimitiveType.Cylinder, "Ballast band", Vector3.zero,
                new Vector3(diameter, .065f, diameter), stripe, detail, false);
            band.SetParent(parent, false);
            band.localPosition = Vector3.up * height;
            return band.gameObject;
        }

        private static void Put(SerializedObject so,string key,UnityEngine.Object item) =>
            so.FindProperty(key).objectReferenceValue = item;
        private static void PutArray(SerializedObject so,string key,params UnityEngine.Object[] items)
        {
            var p = so.FindProperty(key);
            p.arraySize = items.Length;
            for (int i=0;i<items.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void BuildMaterials()
        {
            deck = Mat("Concrete", new Color32(150,168,184,255),.12f,.35f);
            pale = Mat("Anodized top", new Color32(181,194,201,255),.37f,.46f);
            dark = Mat("Girder",new Color32(29,43,59,255),.45f,.38f);
            steel = Mat("Steel",new Color32(68,91,117,255),.54f,.52f);
            black = Mat("Recess",new Color32(15,25,38,255),.18f,.24f);
            stripe = Mat("Safety gold",new Color32(222,157,48,255),.18f,.38f);
            red = Mat("N shell",new Color32(231,29,45,255),.33f,.86f,new Color(.26f,.013f,.018f));
            blue = Mat("S shell",new Color32(19,112,240,255),.33f,.86f,new Color(.007f,.077f,.30f));
            idle = Mat("Unpowered",new Color32(178,105,32,255),.17f,.5f,new Color(.26f,.092f,.018f));
            ready = Mat("Live",new Color32(51,188,251,255),.16f,.53f,new Color(.14f,.8f,1.4f));
            glow = MaterialAsset("Field glow", Shader.Find("Polarity/Magnetic Glow"));
        }

        private static Material MaterialAsset(string name, Shader shader)
        {
            string path = "Assets/Art/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) { material.shader = shader; return material; }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material,path);
            return material;
        }
        private static Material Mat(string name, Color color, float metal, float smooth, Color emission = default)
        {
            Material m = MaterialAsset(name, Shader.Find("Standard"));
            m.color = color;
            m.SetFloat("_Metallic",metal);
            m.SetFloat("_Glossiness",smooth);
            if (emission.maxColorComponent > 0)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor",emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Platform(string name,float x0,float x1,float z0,float z1,float height,float depth,
            float? collisionX0 = null, float? collisionX1 = null)
        {
            float x=(x0+x1)/2,z=(z0+z1)/2,w=x1-x0,d=z1-z0;
            // End the support below the visible deck so both top faces cannot compete for depth.
            const float deckDepth = .44f;
            float supportTop = height - deckDepth + .04f;
            float supportHeight = supportTop - (height - depth);
            Box(name+" chassis",new Vector3(x,supportTop-supportHeight/2,z),
                new Vector3(w-.16f,supportHeight,d-.19f),dark,geometry,false);
            Transform surface = Box(name+" top",new Vector3(x,height-deckDepth/2,z),new Vector3(w,deckDepth,d),deck,geometry,true);
            if (collisionX0.HasValue && collisionX1.HasValue)
            {
                // The joints are continuous to a rolling sphere, even with a visible seam.
                var collider = surface.GetComponent<BoxCollider>();
                collider.size = new Vector3((collisionX1.Value - collisionX0.Value) / w, 1, 1);
                collider.center = new Vector3(((collisionX0.Value + collisionX1.Value) / 2 - x) / w, 0, 0);
            }
            for(int i=0;i<Mathf.CeilToInt(w/1.32f);i++)
            {
                float tileX=x0+.69f+i*1.32f;
                float tileW=Mathf.Min(1.26f,x1-tileX);
                if(tileW<.12f) break;
                for(int side=0;side<3;side++)
                {
                    float depthRow=(d-.36f)/3f;
                    Box("Floor panel",new Vector3(tileX,height+.006f,z0+.18f+(side+.5f)*depthRow),
                        new Vector3(tileW,.012f,depthRow-.032f),i%4==3?deck:pale,detail,false);
                }
                Box("Face rib",new Vector3(tileX,height-depth/2-.12f,z0+.045f),new Vector3(.19f,depth-.28f,.18f),
                    i%3==0?stripe:steel,detail,false);
                if(i%2==0)
                    Box("Painted edge",new Vector3(tileX,height+.018f,z0+.11f),new Vector3(tileW,.023f,.14f),
                        stripe,detail,false);
            }
            Box("Far sill",new Vector3(x,height+.04f,z1-.075f),new Vector3(w,.1f,.14f),steel,detail,false);
            Box("Lower front belt",new Vector3(x,height-.81f,z0+.02f),new Vector3(w-.2f,.22f,.16f),steel,detail,false);
        }

        private static void Frame(Vector3 at, float width, float height, bool cross)
        {
            Vector3 axis = cross ? Vector3.forward : Vector3.right;
            Vector3 pillar = cross ? new Vector3(.55f,height,.38f) : new Vector3(.38f,height,.55f);
            for(int i=-1;i<=1;i+=2)
            {
                Vector3 pt=at+axis*(width/2*i)+Vector3.up*(height/2);
                Box("Frame upright",pt,pillar,dark,detail,false);
                Box("Upright paint",pt+new Vector3(-.04f,.05f,-.08f),pillar*.77f,stripe,detail,false);
                Box("Pedestal foot",pt-Vector3.up*(height/2-.19f),pillar+new Vector3(.23f,.29f,.23f),steel,detail,false);
            }
            Box("Overhead beam",at+Vector3.up*height,cross?new Vector3(.67f,.38f,width+.48f):
                new Vector3(width+.48f,.38f,.67f),dark,detail,false);
        }

        private static Renderer Socket(Vector3 at,float radius,Material lamp,string name)
        {
            Round(name+" outer",at+Vector3.up*.022f,radius*2,.043f,steel,detail);
            Round(name+" emitting rim",at+Vector3.up*.051f,radius*1.83f,.028f,lamp,detail);
            Round(name+" insert",at+Vector3.up*.069f,radius*1.56f,.031f,black,detail);
            Round(name+" hub",at+Vector3.up*.089f,radius*.41f,.022f,pale,detail);
            for(int i=0;i<4;i++)
            {
                float angle=i*Mathf.PI*.5f;
                Box("Terminal lug",at+new Vector3(Mathf.Cos(angle)*radius,.045f,Mathf.Sin(angle)*radius),
                    new Vector3(.15f,.06f,.15f),stripe,detail,false);
            }
            return detail.Find(name+" emitting rim").GetComponent<Renderer>();
        }

        private static void Decorate(bool massChallenge)
        {
            for (int i=0;i<17;i++)
            {
                float x=-22+i*2.85f;
                float z=8.3f+Mathf.Sin(i*2.62f)*4.8f;
                float top=-1.8f-(i%5)*1.76f;
                float h=13.3f+(i%4)*2.2f;
                Box("Distant service tower",new Vector3(x,top-h/2,z),new Vector3(1.8f,h,1.75f),i%3==0?steel:dark,background,false);
                Box("Tower coping",new Vector3(x,top,z),new Vector3(2.16f,.32f,2.13f),steel,background,false);
                if (i%2==0)
                    Box("Vertical status seam",new Vector3(x,top-3.2f,z-.89f),new Vector3(.07f,6.2f,.02f),stripe,background,false);
            }
            for(int i=0;i<9;i++)
                Box("Understructure crossbeam",new Vector3(i*3.2f-12,-8.2f,-2.25f),new Vector3(.22f,.33f,12),steel,background,false);
            Box("Void foundation",new Vector3(0,-19.6f,3),new Vector3(82,.5f,65),steel,background,false);
            if (massChallenge) return;
            Box("Transfer boundary",new Vector3(4.75f,-.29f,-2.46f),new Vector3(2.15f,.66f,.28f),dark,detail,false);
            Box("Boundary accent",new Vector3(4.75f,.058f,-2.46f),new Vector3(2.19f,.04f,.32f),stripe,detail,false);
            Box("Turn guard",new Vector3(7.47f,-.26f,1.25f),new Vector3(2.62f,.72f,.18f),dark,detail,true);
            Box("Turn guard stripe",new Vector3(7.47f,.111f,1.25f),new Vector3(2.63f,.045f,.26f),stripe,detail,false);
        }

        private static Rigidbody Ball(string name, Vector3 at, Material mat,out Renderer shell,out LineRenderer halo,out TrailRenderer trail)
        {
            GameObject ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name=name;
            ball.layer=8;
            ball.transform.position=at;
            ball.transform.localScale=Vector3.one*(MagnetRules.Radius*2);
            shell=ball.GetComponent<Renderer>();
            shell.sharedMaterial=mat;
            var body=ball.AddComponent<Rigidbody>();
            body.mass=1;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.angularDamping=.65f;
            body.maxAngularVelocity=35;
            var friction = new PhysicsMaterial("Rolling shell")
            {
                dynamicFriction=.16f, staticFriction=.22f, bounciness=.04f,
                frictionCombine=PhysicsMaterialCombine.Minimum
            };
            string frictionPath="Assets/Art/Materials/Rolling shell.physicMaterial";
            var previous=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(frictionPath);
            if(previous==null) AssetDatabase.CreateAsset(friction,frictionPath); else friction=previous;
            ball.GetComponent<Collider>().sharedMaterial=friction;
            var seam=Line(name+" meridian",49,.016f);
            seam.transform.SetParent(ball.transform,false);
            seam.useWorldSpace=false;
            seam.startColor=seam.endColor=new Color(.42f,.69f,.85f,.64f);
            var points=new Vector3[49];
            for(int i=0;i<points.Length;i++)
            {
                float a=i*Mathf.PI*2/(points.Length-1);
                points[i]=new Vector3(Mathf.Cos(a)*.507f,Mathf.Sin(a)*.507f,0);
            }
            seam.SetPositions(points);
            var trailObject=new GameObject(name+" motion tail");
            trailObject.transform.SetParent(ball.transform,false);
            trail=trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial=glow;
            trail.time=.32f;
            trail.startWidth=.11f;
            trail.endWidth=.009f;
            trail.emitting=false;
            trail.shadowCastingMode=ShadowCastingMode.Off;
            halo=Line(name+" induction ring",49,.034f);
            return body;
        }

        private static LineRenderer Line(string name,int count,float width)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();
            line.sharedMaterial=glow;
            line.positionCount=count;
            line.widthMultiplier=width;
            line.useWorldSpace=true;
            line.numCornerVertices=3;
            line.shadowCastingMode=ShadowCastingMode.Off;
            return line;
        }

        private static Camera Lighting(bool massChallenge)
        {
            var view=new GameObject("Facility camera").AddComponent<Camera>();
            view.tag="MainCamera";
            view.orthographic=true;
            view.orthographicSize=massChallenge ? 6.05f : 9.25f;
            view.clearFlags=CameraClearFlags.SolidColor;
            view.backgroundColor=new Color32(28,42,64,255);
            view.nearClipPlane=.1f;
            view.farClipPlane=95;
            view.transform.position=massChallenge ? new Vector3(-2.8f,15.9f,-21.8f) : new Vector3(2.7f,15.9f,-21.8f);
            view.transform.LookAt(massChallenge ? new Vector3(-3.8f,-.85f,.4f) : new Vector3(1.5f,-.85f,1.3f));
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.56f,.66f,.79f);
            RenderSettings.ambientEquatorColor=new Color(.33f,.43f,.55f);
            RenderSettings.ambientGroundColor=new Color(.20f,.24f,.38f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.022f;
            RenderSettings.fogColor=view.backgroundColor;
            Directional("Key cold daylight",Quaternion.Euler(50,-36,0),new Color(.83f,.91f,1),1.17f,true);
            Directional("Warm bounce",Quaternion.Euler(22,145,0),new Color(1,.66f,.43f),.58f,false);
            return view;
        }

        private static void Directional(string name,Quaternion rotation,Color color,float intensity,bool shadows)
        {
            var light=new GameObject(name).AddComponent<Light>();
            light.type=LightType.Directional;
            light.color=color;
            light.intensity=intensity;
            light.shadows=shadows?LightShadows.Soft:LightShadows.None;
            light.transform.rotation=rotation;
        }
        private static Transform Mark(string name,Vector3 at)
        {
            var marker=new GameObject(name).transform;
            marker.SetParent(detail);
            marker.position=at;
            return marker;
        }
        private static Transform Box(string name,Vector3 at,Vector3 size,Material mat,Transform parent,bool collider)
            => Primitive(PrimitiveType.Cube,name,at,size,mat,parent,collider);
        private static void Round(string name,Vector3 at,float diameter,float height,Material mat,Transform parent)
            => Primitive(PrimitiveType.Cylinder,name,at,new Vector3(diameter,height*.5f,diameter),mat,parent,false);
        private static Transform Primitive(PrimitiveType type,string name,Vector3 at,Vector3 size,Material mat,Transform parent,bool collider)
        {
            var item=GameObject.CreatePrimitive(type);
            item.name=name;
            item.transform.SetParent(parent);
            item.transform.position=at;
            item.transform.localScale=size;
            item.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item.transform;
        }
        private static void LocalBox(Transform parent,string name,Vector3 at,Vector3 size,Material mat)
        {
            Transform part=Box(name,Vector3.zero,size,mat,detail,false);
            part.SetParent(parent,false);
            part.localPosition=at;
            part.localScale=size;
        }

        public static void BuildWindows()
        {
            FacilityLab.Verify();
            CreateScene();
            PlayerSettings.productName="Polarity Facility";
            PlayerSettings.companyName="Game Programming Classroom";
            PlayerSettings.defaultScreenWidth=1280;
            PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.defaultIsNativeResolution=false;
            QualitySettings.antiAliasing=4;
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.build-v2/player"));
            Directory.CreateDirectory(dir);
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},Path.Combine(dir,"PolarityFacility.exe"),
                BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed " + report.summary.result);
        }

        [MenuItem("Tools/Polarity Facility/Build Mass Transfer for Windows")]
        public static void BuildMassTransferWindows() => BuildChallengeWindows(MassScenePath, "Windows-MassTransfer");

        [MenuItem("Tools/Polarity Facility/Build All Levels for Windows")]
        public static void BuildCampaignWindows() =>
            BuildWindowsPlayer(new[] { MenuScenePath, ScenePath, MassScenePath, RailScenePath }, "Windows-Campaign");

        private static void BuildChallengeWindows(string scenePath, string folder) =>
            BuildWindowsPlayer(new[] { scenePath }, folder);

        private static void BuildWindowsPlayer(string[] scenes, string folder)
        {
            FacilityLab.Verify();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build", folder));
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(scenes, Path.Combine(dir, "PolarityFacility.exe"),
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed for {folder}: {report.summary.result}");
        }
    }
}
