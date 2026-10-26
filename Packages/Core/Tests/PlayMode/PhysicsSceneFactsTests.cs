// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Spellbound.Core.PlayModeTests {
    public class PhysicsSceneFactsTests {
        private const string LocalSceneName = "SpellboundCoreFactsLocalPhysics";
        private const int StepsToWatch = 25;
        private const float StartHeight = 10f;

        private sealed class StepLog {
            public readonly List<string> Entries = new List<string>();
        }

        [DefaultExecutionOrder(-32000)]
        private sealed class EarliestStepProbe : MonoBehaviour {
            public StepLog Log;

            private void FixedUpdate() {
                Log.Entries.Add("earliest");
            }
        }

        [DefaultExecutionOrder(32000)]
        private sealed class LatestStepProbe : MonoBehaviour {
            public StepLog Log;
            public PhysicsScene Scene;
            public int Steps;

            private void FixedUpdate() {
                Scene.Simulate(Time.fixedDeltaTime);
                Steps++;
                Log.Entries.Add("latest");
            }
        }

        private Scene _localScene;
        private readonly List<GameObject> _created = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator CreateLocalScene() {
            _localScene = SceneManager.CreateScene(LocalSceneName,
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadLocalScene() {
            foreach (var created in _created) {
                if (created != null) {
                    Object.Destroy(created);
                }
            }

            _created.Clear();

            if (_localScene.IsValid() && _localScene.isLoaded) {
                yield return SceneManager.UnloadSceneAsync(_localScene);
            }
        }

        [UnityTest]
        public IEnumerator LocalPhysicsModeSceneOwnsItsPhysicsAndNothingStepsIt() {
            var localPhysics = _localScene.GetPhysicsScene();
            Assert.AreNotEqual(Physics.defaultPhysicsScene, localPhysics);
            var localBody = Sphere("local body", _localScene);
            var defaultBody = Sphere("default body", SceneManager.GetActiveScene());

            for (var step = 0; step < StepsToWatch; step++) {
                yield return new WaitForFixedUpdate();
            }

            var localHeight = localBody.transform.position.y;
            var defaultHeight = defaultBody.transform.position.y;
            Debug.Log("PhysicsSceneFactsTests: on " + Application.unityVersion + " after " + StepsToWatch
                      + " fixed steps a body in a LocalPhysicsMode scene nobody steps sits at " + localHeight
                      + " while a body in the default scene fell to " + defaultHeight);
            Assert.AreEqual(StartHeight, localHeight, 0.001f);
            Assert.Less(defaultHeight, StartHeight - 0.5f);

            localPhysics.Simulate(Time.fixedDeltaTime);
            yield return null;
            Assert.Less(localBody.transform.position.y, StartHeight);
        }

        [UnityTest]
        public IEnumerator SimulateFromLatestOrderFixedUpdateStepsThatSceneAfterEarlierScripts() {
            var localPhysics = _localScene.GetPhysicsScene();
            var localBody = Sphere("stepped body", _localScene);
            var log = new StepLog();
            var host = new GameObject("facts probes");
            _created.Add(host);
            host.AddComponent<EarliestStepProbe>().Log = log;
            var latest = host.AddComponent<LatestStepProbe>();
            latest.Log = log;
            latest.Scene = localPhysics;

            for (var step = 0; step < StepsToWatch; step++) {
                yield return new WaitForFixedUpdate();
            }

            Assert.Greater(latest.Steps, 0);
            Assert.AreEqual(0, log.Entries.Count % 2);

            for (var index = 0; index < log.Entries.Count; index += 2) {
                Assert.AreEqual("earliest", log.Entries[index]);
                Assert.AreEqual("latest", log.Entries[index + 1]);
            }

            var height = localBody.transform.position.y;
            Debug.Log("PhysicsSceneFactsTests: on " + Application.unityVersion
                      + " PhysicsScene.Simulate(Time.fixedDeltaTime) from a DefaultExecutionOrder(32000) FixedUpdate"
                      + " ran " + latest.Steps + " times after a DefaultExecutionOrder(-32000) FixedUpdate each step,"
                      + " threw nothing, and the body in that scene fell from " + StartHeight + " to " + height);
            Assert.Less(height, StartHeight - 0.5f);
        }

        private GameObject Sphere(string name, Scene scene) {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = new Vector3(0f, StartHeight, 0f);
            sphere.AddComponent<Rigidbody>();
            SceneManager.MoveGameObjectToScene(sphere, scene);
            _created.Add(sphere);
            return sphere;
        }
    }
}
