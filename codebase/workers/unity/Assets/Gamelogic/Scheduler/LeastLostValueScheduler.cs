﻿using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Orders;
using Improbable.Metrics;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LeastLostValueScheduler : MonoBehaviour, Scheduler
{
	[Require]
    private ControllerMetrics.Writer MetricsWriter;

	[Require]
    private DeliveryHandler.Writer DeliveryHandlerWriter;

	SortedSet<QueueEntry> requestQueue;

	int incomingRequests;

	float potential;
    int rejections;

    // Use this for initialization
    private void OnEnable()
    {
        incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
        potential = DeliveryHandlerWriter.Data.potential;
        rejections = DeliveryHandlerWriter.Data.rejections;

		requestQueue = new SortedSet<QueueEntry>(new LLVComparer());

		foreach (QueueEntry entry in DeliveryHandlerWriter.Data.requestQueue)
        {
			requestQueue.Add(entry);
        }

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);
	}

	private void OnDisable()
	{
		requestQueue.Clear();

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

	public void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetIncomingDeliveryRequests(++incomingRequests));

		float estimatedTime = Vector3.Distance(gameObject.transform.position, handle.Request.destination.ToUnityVector()) / SimulationSettings.MaxDroneSpeed;
		QueueEntry queueEntry = new QueueEntry(Time.time, handle.Request, 0, estimatedTime);
		requestQueue.Add(queueEntry);
        handle.Respond(new DeliveryResponse(true));
    }

	private float ExpectedValue(QueueEntry queueEntry)
	{
		return ExpectedValue(queueEntry.expectedDuration, queueEntry.request.packageInfo, queueEntry.request.timeValueFunction);
	}
    
	private float ExpectedValue(float estimatedTime, PackageInfo packageInfo, TimeValueFunction tvf)
	{
		return (float)TimeValueFunctions.DeliveryValue(estimatedTime, packageInfo, tvf);
	}
    
	void Scheduler.UpdateDeliveryRequestQueue()
    {
		Improbable.Collections.List<QueueEntry> queueList = new Improbable.Collections.List<QueueEntry>();
		foreach (QueueEntry entry in requestQueue)
        {
			queueList.Add(entry);
        }

		DeliveryHandlerWriter.Send(new DeliveryHandler.Update()
		                           .SetRequestQueue(queueList)
		                           .SetPotential(potential)
		                           .SetRejections(rejections));
    }

	int Scheduler.GetQueueSize()
	{
		return requestQueue.Count;
	}

	float Scheduler.GetPotentialLost()
    {
        return potential;
    }

    float Scheduler.GetAvgPotentialLost()
    {
        return potential / rejections;
    }

	private void SortQueue()
	{
		QueueEntry[] entries = new QueueEntry[requestQueue.Count];
		int i = 0;
		foreach(QueueEntry entry in requestQueue)
		{
			entries[i++] = entry.DeepCopy();
		}

		for (int j = 0; j < entries.Length; j++)
		{
			float lostValue = 0;
			float maxDuration = float.MinValue;
			float entryDuration = entries[j].expectedDuration;
			float timePassed;

			for (int k = 0; k < entries.Length; k++)
			{
				if (j != k) {
					//timePassed = wait time so far + estimated time til the delivery
					timePassed = Time.time - entries[k].timestamp + entries[k].expectedDuration;
					lostValue += ExpectedValue(timePassed, entries[k].request.packageInfo, entries[k].request.timeValueFunction)
						       - ExpectedValue(timePassed + entryDuration, entries[j].request.packageInfo, entries[j].request.timeValueFunction);

					maxDuration = Mathf.Max(maxDuration, entries[k].expectedDuration);
				}
			}

			//timePassed = wait time so far + estimated time til the delivery
			timePassed = Time.time - entries[j].timestamp + entries[j].expectedDuration;
			float wonValue = ExpectedValue(timePassed, entries[j].request.packageInfo, entries[j].request.timeValueFunction)
				           - ExpectedValue(timePassed + maxDuration, entries[j].request.packageInfo, entries[j].request.timeValueFunction);

			entries[j].priority = lostValue - wonValue;
		}

		requestQueue.Clear();
		foreach(QueueEntry entry in entries)
		{
			requestQueue.Add(entry);
		}
	}

	bool Scheduler.GetNextRequest(out QueueEntry queueEntry)
    {
		if (requestQueue.Count > 0)
		{
			SortQueue();
			while(requestQueue.Count > SimulationSettings.MaxDeliveryRequestQueueSize)
			{
				QueueEntry minEntry = requestQueue.Min;
				float duration = Time.time - minEntry.timestamp + minEntry.expectedDuration;
				float value = ExpectedValue(duration, minEntry.request.packageInfo, minEntry.request.timeValueFunction);
				requestQueue.Remove(minEntry);

				potential += value;
                ++rejections;
			}

			queueEntry = requestQueue.Max;
			requestQueue.Remove(queueEntry);
			return true;
		}

		queueEntry = new QueueEntry();
		return false;
    }
}

class LLVComparer : IComparer<QueueEntry>
{
	public int Compare(QueueEntry x, QueueEntry y)
	{
		//a > b ==> 1
        //a < b ==> -1
        //a == b ==> 0

        //two entries are same if matching priority AND timestamp
		if (x.priority == y.priority && x.timestamp == y.timestamp)
		{
			return 0;
		}

        //order by priority
		if (x.priority > y.priority)
		{
			return 1;
		}

		return -1;
	}
}