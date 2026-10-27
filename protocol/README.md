# Protocol overview

## Pairing mode

When devices are in the pairing mode, they send `Capabilities` payload (with
some interval) constantly. When the `Hello` downlink is received, they enter
**normal operation** mode.

> [!NOTE]
>
> It should be noted that they can expect all of the downlinks specified, but
> the only one they can respond to with some action is `Hello` payload.

## Normal operation mode

During this mode devices are sending their `Report` payload regularly with some
interval (_Or when specified by `wake_after_secs`, which is described later,
after some established time once_).

In normal mode they can respond to all of the specified downlink payloads:

- `Hello` - _not used currently_
- `Action` - requires the device to take some action, which was exposed by the
  device itself
- `Disconnect` - disconnect the device from the application, which is forcing
  the device to turn to **Pairing mode** once again

### wake_after_secs

When the application is calling some device with an action, it can specify the
`wake_aftes_secs` arguments which is instructing the device to wake after
specified amount of seconds (integer value).

After the specified time, the device is waking up and sending another uplink
with `Hello` payload, so the application can take advantage of open downlink
window to send another action

### Application specific reject

Note that _reject_ or _block_ operations from the application point of view, are
not part of the protocol. These are application limited mechanisms for manual
control of devices during the pairing mode. One would want to ignore or reject
some pairing devices and this operation will not intervene with the device
itself
