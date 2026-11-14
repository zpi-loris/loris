import tomllib
import argparse
import yaml
import json
import requests

parser = argparse.ArgumentParser(
    prog="loris CLI",
    description="Use this tool to communicate with the loris client app via CLI",
)

base_url = "http://localhost:5158/api"
# opts
# - view N last readings (optional)
# - view N last readings from sensor (optional)
# - parse automation
#   - validate (sth like terraform plan)
#   - apply
# - view capabilities of the device
# - list devices
# - disconnect device
# - welcome the device
# - ignore(revert) the device


def accept(args):
    if args.by_eui:
        request_url = f"{base_url}/devices/by-eui/{args.device}/accept"
    else:
        request_url = f"{base_url}/devices/by-name/{args.device}/accept"

    r = requests.post(request_url)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return
    print(f"Device {args.device} accepted successfully!")
    print(r.content)


def disconnect(args):
    request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    r = requests.post(request_url)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return
    print(f"Device {args.device} disconnected")


def ignore(args):
    if args.revert:
        request_url = f"{base_url}/devices/by-eui/{args.device}/accept"
    else:
        request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    r = requests.post(request_url)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return

    if args.revert:
        print(f"Device {args.device} ignored")
    else:
        print(f"Device {args.device} is no longer ignored")


def list_devices():
    request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    if args.pending:
        pass
    elif args.accepted:
        pass
    elif args.ignored:
        pass
    elif args.blocked:
        pass
    else:
        pass

    r = requests.post(request_url)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return


def device(args):
    request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    r = requests.post(request_url)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return
    print(r.content)


def apply(args):
    request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    with open(args.filepath, "r") as yaml_in:
        yaml_object = yaml.safe_load(yaml_in)
    json_string = json.dumps(yaml_object, default=str)

    r = requests.post(request_url, json_string)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return


def validate():
    request_url = f"{base_url}/devices/by-eui/{args.device}/accept"

    with open(args.filepath, "r") as yaml_in:
        yaml_object = yaml.safe_load(yaml_in)
    json_string = json.dumps(yaml_object, default=str)

    r = requests.post(request_url, json_string)
    if r.status_code != 200:
        print(f"ERROR: Status Code {r.status_code}")
        return


subparsers = parser.add_subparsers(help="Subcommand help")

# Accept
accept_subparser = subparsers.add_parser("accept", help="Accept the device")
accept_subparser.add_argument(
    "device", type=str, help="Name (default) or the EUI (-d option) of the device"
)
accept_subparser.add_argument(
    "-d", "--by-eui", help="Accept by the EUI of the device", action="store_true"
)
accept_subparser.set_defaults(func=accept)

# Disconnect
disconnect_subparser = subparsers.add_parser("disconnect", help="Disconnect the device")
disconnect_subparser.add_argument("device", type=str, help="EUI of the device")
disconnect_subparser.add_argument(
    "device", type=str, help="Name (default) or the EUI (-d option) of the device"
)
disconnect_subparser.add_argument(
    "-d", "--by-eui", help="Accept by the EUI of the device", action="store_true"
)
disconnect_subparser.set_defaults(func=disconnect)

# Ignore
ignore_subparser = subparsers.add_parser("ignore", help="Ignore the device")
ignore_subparser.add_argument(
    "-d", "--by-eui", help="Accept by the EUI of the device", action="store_true"
)
ignore_subparser.add_argument(
    "-r", "--revert", help="Revert the ignore", action="store_true"
)
ignore_subparser.add_argument(
    "device", type=str, help="Name (default) or the EUI (-d option) of the device"
)
ignore_subparser.set_defaults(func=ignore)

# List devices
list_subparser = subparsers.add_parser("list", help="List devices")
list_subparser.set_defaults(func=list_devices)
list_subparser.add_argument(
    "-p", "--pending", help="List pending devices", action="store_true"
)
list_subparser.add_argument(
    "-c", "--connected", help="List connected devices", action="store_true"
)
list_subparser.add_argument(
    "-i", "--ignored", help="List ignored devices", action="store_true"
)
list_subparser.add_argument(
    "-b", "--blocked", help="List blocked devices", action="store_true"
)
list_subparser.set_defaults(func=list_devices)

# Device info
device_subparser = subparsers.add_parser(
    "device", help="Show information about the device"
)
device_subparser.add_argument("DevEUI", type=str, help="DevEUI of the device")

# Validate
validate_subparser = subparsers.add_parser(
    "validate", help="Validate the automation file"
)
validate_subparser.add_argument(
    "filepath", type=str, help="Path to the automation file"
)
validate_subparser.set_defaults(func=validate)

# Apply
apply_subparser = subparsers.add_parser("apply", help="Apply the automation file")
apply_subparser.add_argument("filepath", type=str, help="Path to the automation file")
apply_subparser.set_defaults(func=apply)

args = parser.parse_args()
print(args)

if "func" in args:
    args.func(args)
else:
    parser.print_help()
