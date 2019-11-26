﻿/* Copyright © 2014 Apex Software. All rights reserved. */
namespace Apex.LoadBalancing
{
    /// <summary>
    /// Load balancer interface
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// Gets the default update interval to use for items where a specific interval is not specified.
        /// </summary>
        /// <value>
        /// The default update interval.
        /// </value>
        float defaultUpdateInterval
        {
            get;
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        void Add(ILoadBalanced item);

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="delayFirstUpdate">if set to <c>true</c> the item will be scheduled for its first update <see cref="defaultUpdateInterval"/> into the future, otherwise its first update will be as soon as possible.</param>
        void Add(ILoadBalanced item, bool delayFirstUpdate);

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="interval">The interval between updates. This overrides the <see cref="defaultUpdateInterval"/>.</param>
        void Add(ILoadBalanced item, float interval);

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="interval">The interval between updates. This overrides the <see cref="defaultUpdateInterval"/>.</param>
        /// <param name="delayFirstUpdate">if set to <c>true</c> the item will be scheduled for its first update <paramref name="interval"/> into the future, otherwise its first update will be as soon as possible.</param>
        void Add(ILoadBalanced item, float interval, bool delayFirstUpdate);

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="interval">The interval between updates. This overrides the <see cref="defaultUpdateInterval"/>.</param>
        /// <param name="delayFirstUpdateBy">The delay by which the first update of the item will be scheduled.</param>
        void Add(ILoadBalanced item, float interval, float delayFirstUpdateBy);

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        void Remove(ILoadBalanced item);
    }
}
