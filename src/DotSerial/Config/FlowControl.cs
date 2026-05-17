// -----------------------------------------------------------------------
// <copyright file="FlowControl.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Flow-control (handshake) protocol for serial communication.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Config;

/// <summary>Flow-control (handshake) protocol.</summary>
public enum FlowControl
{
    /// <summary>No flow control.</summary>
    None,
    /// <summary>XOn/XOff software flow control.</summary>
    XOnXOff,
    /// <summary>RTS/CTS hardware flow control.</summary>
    RequestToSend,
    /// <summary>RTS/CTS and XOn/XOff combined flow control.</summary>
    RequestToSendXOnXOff,
}
