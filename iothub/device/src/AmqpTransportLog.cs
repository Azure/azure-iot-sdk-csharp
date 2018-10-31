using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public class AmqpTransportLog : AmqpTrace
    {
        public override void AmqpAbortThrowingException(string exception)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpAbortThrowingException:", "AmqpTransportLog");
            base.AmqpAbortThrowingException(exception);
        }

        public override void AmqpAddSession(object source, object session, ushort localChannel, ushort remoteChannel)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpAddSession:", $"{" session:"}{session}.{" localChannel:"}.{localChannel}.{" remoteChannel:"}.{remoteChannel}");
            base.AmqpAddSession(source, session, localChannel, remoteChannel);
        }

        public override void AmqpAttachLink(object connection, object session, object link, uint localHandle, uint remoteHandle, string linkName, string role, object source, object target)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpAttachLink", $"{" session:"}{session}.{" link:"}.{link}.{" localHandle:"}.{localHandle}.{" remoteHandle:"}.{remoteHandle}.{" linkName:"}.{linkName}.{" role:"}.{role}.{" source:"}.{source}.{" target:"}.{target}");
            base.AmqpAttachLink(connection, session, link, localHandle, remoteHandle, linkName, role, source, target);
        }

        public override void AmqpCacheMessage(object source, uint deliveryId, int count, bool isPrefecthingBySize, long totalCacheSizeInBytes, uint totalLinkCredit, uint linkCredit)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpCacheMessage:", $"{" source:"}{source}.{" deliveryId:"}.{deliveryId}.{" count:"}.{count}.{" isPrefecthingBySize:"}.{isPrefecthingBySize}.{" totalCacheSizeInBytes:"}.{totalCacheSizeInBytes}.{" totalLinkCredit:"}.{totalLinkCredit}.{" linkCredit:"}.{linkCredit}");
            base.AmqpCacheMessage(source, deliveryId, count, isPrefecthingBySize, totalCacheSizeInBytes, totalLinkCredit, linkCredit);
        }

        public override void AmqpCloseConnection(object source, object connection, bool abort)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpCloseConnection:", $"{" source:"}{source}.{" connection:"}.{connection}.{" abort:"}.{abort}");
            base.AmqpCloseConnection(source, connection, abort);
        }

        public override void AmqpDeliveryNotFound(object source, string deliveryTag)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpDeliveryNotFound:", $"{" source:"}{source}.{" deliveryTag:"}.{deliveryTag}");
            base.AmqpDeliveryNotFound(source, deliveryTag);
        }

        public override void AmqpDispose(object source, uint deliveryId, bool settled, object state)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpDispose:", $"{" source:"}{source}.{" deliveryId:"}.{deliveryId}.{" settled:"}.{settled}.{" state:"}.{state}");
            base.AmqpDispose(source, deliveryId, settled, state);
        }

        public override void AmqpDynamicBufferSizeChange(object source, string type, int oldSize, int newSize)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpDynamicBufferSizeChange:", $"{" source:"}{source}.{" type:"}.{type}.{" oldSize:"}.{oldSize}.{" newSize:"}.{newSize}");
            base.AmqpDynamicBufferSizeChange(source, type, oldSize, newSize);
        }

        public override void AmqpHandleException(Exception exception, string traceInfo)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpHandleException:", $"{" exception:"}{exception}.{" traceInfo:"}.{traceInfo}");
            base.AmqpHandleException(exception, traceInfo);
        }

        public override void AmqpInsecureTransport(object source, object transport, bool isSecure, bool isAuthenticated)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpInsecureTransport:", $"{" source:"}{source}.{" transport:"}.{transport}.{" isSecure:"}.{isSecure}.{" isAuthenticated:"}.{isAuthenticated}");
            base.AmqpInsecureTransport(source, transport, isSecure, isAuthenticated);
        }

        public override void AmqpIoEvent(object source, int ioEvent, long queueSize)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpIoEvent:", $"{" source:"}{source}.{" ioEvent:"}.{ioEvent}.{" queueSize:"}.{queueSize}");
            base.AmqpIoEvent(source, ioEvent, queueSize);
        }

        public override void AmqpLinkDetach(object source, string name, uint handle, string action, string error)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpLinkDetach", $"{" source:"}{source}.{" name:"}.{name}.{" handle:"}.{handle}.{" action:"}.{action}.{" error:"}.{error}");
            base.AmqpLinkDetach(source, name, handle, action, error);
        }

        public override void AmqpListenSocketAcceptError(object source, bool willRetry, string error)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpListenSocketAcceptError:", $"{" source:"}{source}.{" willRetry:"}.{willRetry}.{" error:"}.{error}");
            base.AmqpListenSocketAcceptError(source, willRetry, error);
        }

        public override void AmqpLogError(object source, string operation, string message)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpLogError:", $"{" source:"}{source}.{" operation:"}.{operation}.{" message:"}.{message}");
            base.AmqpLogError(source, operation, message);
        }

        public override void AmqpLogOperationInformational(object source, TraceOperation operation, object detail)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpLogOperationInformational:", $"{" source:"}{source}.{" operation:"}.{operation}.{" detail:"}.{detail}");
            base.AmqpLogOperationInformational(source, operation, detail);
        }

        public override void AmqpLogOperationVerbose(object source, TraceOperation operation, object detail)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpLogOperationVerbose:", $"{" source:"}{source}.{" operation:"}.{operation}.{" detail:"}.{detail}");
            base.AmqpLogOperationVerbose(source, operation, detail);
        }

        public override void AmqpMissingHandle(object source, string type, uint handle)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpMissingHandle:", $"{" source:"}{source}.{" type:"}.{type}.{" handle:"}.{handle}");
            base.AmqpMissingHandle(source, type, handle);
        }

        public override void AmqpOpenConnection(object source, object connection)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpOpenConnection:", $"{" source:"}{source}.{" connection:"}.{connection}");
            base.AmqpOpenConnection(source, connection);
        }

        public override void AmqpOpenEntityFailed(object source, object obj, string name, string entityName, string error)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpOpenEntityFailed:", $"{" source:"}{source}.{" obj:"}.{obj}.{" name:"}.{name}.{" entityName:"}.{entityName}.{" error:"}.{error}");
            base.AmqpOpenEntityFailed(source, obj, name, entityName, error);
        }

        public override void AmqpOpenEntitySucceeded(object source, object obj, string name, string entityName)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpOpenEntitySucceeded:", $"{" source:"}{source}.{" obj:"}.{obj}.{" entityName:"}.{entityName}");
            base.AmqpOpenEntitySucceeded(source, obj, name, entityName);
        }

        public override void AmqpReceiveMessage(object source, uint deliveryId, int transferCount)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpReceiveMessage:", $"{" source:"}{source}.{" deliveryId:"}.{deliveryId}.{" transferCount:"}.{transferCount}");
            base.AmqpReceiveMessage(source, deliveryId, transferCount);
        }

        public override void AmqpRemoveLink(object connection, object session, object link, uint localHandle, uint remoteHandle, string linkName)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpRemoveLink", $"{" connection:"}{connection}.{" session:"}.{session}.{" link"}.{link}.{" localHandle:"}.{localHandle}.{" remoteHandle:"}.{remoteHandle}.{" linkName:"}.{linkName}");
            base.AmqpRemoveLink(connection, session, link, localHandle, remoteHandle, linkName);
        }

        public override void AmqpRemoveSession(object source, object session, ushort localChannel, ushort remoteChannel)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpRemoveSession:", $"{" source:"}{source}.{" session:"}.{session}.{" localChannel:"}.{localChannel}.{" remoteChannel:"}.{remoteChannel}");
            base.AmqpRemoveSession(source, session, localChannel, remoteChannel);
        }

        public override void AmqpSessionWindowClosed(object source, int nextId)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpSessionWindowClosed:", $"{" source:"}{source}.{" nextId:"}.{nextId}");
            base.AmqpSessionWindowClosed(source, nextId);
        }

        public override void AmqpStateTransition(object source, string operation, object fromState, object toState)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpStateTransition:", $"{" source:"}{source}.{" operation:"}.{operation}.{" fromState:"}.{fromState}.{" toState:"}.{toState}");
            base.AmqpStateTransition(source, operation, fromState, toState);
        }

        public override void AmqpUpgradeTransport(object source, object from, object to)
        {
            if (Logging.IsEnabled) Logging.Info(this, "AmqpUpgradeTransport:", $"{" source:"}{source}.{" from:"}.{from}.{" to:"}.{to}");
            base.AmqpUpgradeTransport(source, from, to);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
