using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Worker;
using Improbable.Controller;
using Improbable.Orders;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class LondonSnapshot : MonoBehaviour
    {

        [MenuItem("Drone Sim/Profiling Snapshot")]
        private static void ProfilingSnapshot()
        {
            float maxX = 400;
            float maxZ = 400;

            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 2; //reserve id 1 for the scheduler

            Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones = new Improbable.Collections.List<Improbable.Controller.NoFlyZone>();
            Improbable.Collections.List<ControllerInfo> controllers = new Improbable.Collections.List<ControllerInfo>();

            // CONTROLLERS
            int firstController = currentEntityId;
            EntityId controllerId = new EntityId(currentEntityId++);
            Coordinates controllerPos = new Coordinates(100, 0, 100);
            controllers.Add(new ControllerInfo(controllerId, controllerPos.ToSpatialVector3f()));
            snapshotEntities.Add(
                controllerId,
                EntityTemplateFactory.CreateControllerTemplate(
                    controllerPos,
                    new Vector3f(-maxX, 0, maxZ),
                    new Vector3f(maxX, 0, -maxZ),
                    noFlyZones,
					PopulateControllerSlots()
            ));
            int lastController = currentEntityId;
            // controller placement complete

            // ORDER GENERATOR 
            // find and place order generator
			OrderGeneratorBehaviour orderGenerator = FindObjectOfType<OrderGeneratorBehaviour>();
            snapshotEntities.Add(
				SimulationSettings.OrderGeneratorEntityId,
                EntityTemplateFactory.CreateSchedulerTemplate(
                    new Vector3(0, 0, 0),
                    firstController,
                    lastController,
                    noFlyZones,
                    controllers
                )
            );
            // end scheduler placement

            SnapshotMenu.SaveSnapshot(snapshotEntities, "profiling");
        }

        [MenuItem("Drone Sim/London Snapshot LARGE")]
        private static void LondonLarge()
        {
            float maxX = SimulationSettings.maxX; //routable width is 31500m
            float maxZ = SimulationSettings.maxZ; //routable height is 14000m

            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 2; //reserve id 1 for the scheduler

            Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones = new Improbable.Collections.List<Improbable.Controller.NoFlyZone>();
            Improbable.Collections.List<ControllerInfo> controllers = new Improbable.Collections.List<ControllerInfo>();

            // NO FLY ZONES
            // start creating no fly zones from the editor
            NFZScript[] noFlyZoneScripts = FindObjectsOfType<NFZScript>();
            foreach (NFZScript noFlyZoneScript in noFlyZoneScripts)
            {
                noFlyZones.Add(noFlyZoneScript.GetNoFlyZone());
            }
            // end creation of no fly zones

            // CONTROLLERS
            // start placing controllers
            int firstController = currentEntityId;
            ControllerBehaviour[] controllerScripts = FindObjectsOfType<ControllerBehaviour>();
            foreach (ControllerBehaviour controllerScript in controllerScripts)
            {
                EntityId controllerId = new EntityId(currentEntityId++);
                Coordinates controllerPos = controllerScript.gameObject.transform.position.ToCoordinates();

                controllers.Add(new ControllerInfo(controllerId, controllerPos.ToSpatialVector3f()));

                snapshotEntities.Add(
                    controllerId,
                    EntityTemplateFactory.CreateControllerTemplate(
                        controllerPos,
                        new Vector3f(-maxX, 0, maxZ),
                        new Vector3f(maxX, 0, -maxZ),
                        noFlyZones,
						PopulateControllerSlots()
                ));
            }
            int lastController = currentEntityId;
            // controller placement complete

            // make nfz nodes show up on the inspector map
            //currentEntityId = ShowNoFlyZones(noFlyZones, snapshotEntities, currentEntityId);

            // ORDER GENERATOR 
            // find and place order generator
			OrderGeneratorBehaviour orderGenerator = FindObjectOfType<OrderGeneratorBehaviour>();
            snapshotEntities.Add(
				SimulationSettings.OrderGeneratorEntityId,
                EntityTemplateFactory.CreateSchedulerTemplate(
					orderGenerator.gameObject.transform.position,
                    firstController,
                    lastController,
                    noFlyZones,
                    controllers
                )
            );
            // end scheduler placement

            SnapshotMenu.SaveSnapshot(snapshotEntities, "london_large");
        }

		[MenuItem("Drone Sim/London Snapshot SMALL")]
        private static void LondonSmall()
        {
            float maxX = SimulationSettings.maxX; //routable width is 31500m
            float maxZ = SimulationSettings.maxZ; //routable height is 14000m

            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 2; //reserve id 1 for the scheduler

            Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones = new Improbable.Collections.List<Improbable.Controller.NoFlyZone>();
            Improbable.Collections.List<ControllerInfo> controllers = new Improbable.Collections.List<ControllerInfo>();

            // NO FLY ZONES
            // start creating no fly zones from the editor
            NFZScript[] noFlyZoneScripts = FindObjectsOfType<NFZScript>();
            foreach (NFZScript noFlyZoneScript in noFlyZoneScripts)
            {
                noFlyZones.Add(noFlyZoneScript.GetNoFlyZone());
            }
            // end creation of no fly zones

            // CONTROLLERS
            // start placing controllers
            int firstController = currentEntityId;
            ControllerBehaviour[] controllerScripts = FindObjectsOfType<ControllerBehaviour>();
            foreach (ControllerBehaviour controllerScript in controllerScripts)
            {
                EntityId controllerId = new EntityId(currentEntityId++);
                Coordinates controllerPos = controllerScript.gameObject.transform.position.ToCoordinates();

                controllers.Add(new ControllerInfo(controllerId, controllerPos.ToSpatialVector3f()));

                snapshotEntities.Add(
                    controllerId,
                    EntityTemplateFactory.CreateControllerTemplate(
                        controllerPos,
                        new Vector3f(-maxX, 0, maxZ),
                        new Vector3f(maxX, 0, -maxZ),
                        noFlyZones,
                        PopulateControllerSlots()
                ));
            }
            int lastController = currentEntityId;
            // controller placement complete

            // make nfz nodes show up on the inspector map
            //currentEntityId = ShowNoFlyZones(noFlyZones, snapshotEntities, currentEntityId);

            // ORDER GENERATOR 
            // find and place order generator
            OrderGeneratorBehaviour orderGenerator = FindObjectOfType<OrderGeneratorBehaviour>();
            snapshotEntities.Add(
                SimulationSettings.OrderGeneratorEntityId,
                EntityTemplateFactory.CreateSchedulerTemplate(
                    orderGenerator.gameObject.transform.position,
                    firstController,
                    lastController,
                    noFlyZones,
                    controllers
                )
            );
            // end scheduler placement

            SnapshotMenu.SaveSnapshot(snapshotEntities, "london_small");
        }

		private static DroneInfo GenerateDroneInfo()
		{
			return new DroneInfo(false, new Improbable.EntityId(-1), SimulationSettings.MaxDroneBattery);
		}

		private static Improbable.Collections.List<DroneInfo> PopulateControllerSlots()
		{
			Improbable.Collections.List<DroneInfo> droneSlots = new Improbable.Collections.List<DroneInfo>((int)SimulationSettings.MaxDroneCountPerController);
			while (droneSlots.Count < droneSlots.Capacity)
			{
				droneSlots.Add(GenerateDroneInfo());
			}
			return droneSlots;
		}

        private static int ShowNoFlyZones(Improbable.Controller.NoFlyZone noFlyZone, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            foreach (Vector3f vertex in noFlyZone.vertices)
            {
                snapshotEntities.Add(
                    new EntityId(currentEntityId++),
                    EntityTemplateFactory.CreateNfzNodeTemplate(vertex.ToCoordinates())
                );
            }

            return currentEntityId;
        }

        private static int ShowNoFlyZones(Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            foreach (Improbable.Controller.NoFlyZone noFlyZone in noFlyZones)
            {
                currentEntityId = ShowNoFlyZones(noFlyZone, snapshotEntities, currentEntityId);
            }

            return currentEntityId;
        }
    }
}