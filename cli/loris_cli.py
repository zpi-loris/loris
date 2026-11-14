import argparse
import yaml
import json

parser = argparse.ArgumentParser(
    prog="loris CLI",
    description="Use this tool to communicate with the loris client app via CLI",
)

base = "http://localhost:5158"

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


def accept():
    pass


def disconnect():
    pass


def ignore():
    pass


def list_devices():
    pass


def apply():
    pass


def validate():
    pass


subparsers = parser.add_subparsers(help="Subcommand help")

# Accept
accept_subparser = subparsers.add_parser("accept", help="Accept the device")
accept_subparser.add_argument("DevEUI", type=str, help="DevEUI of the device")

# Disconnect
disconnect_subparser = subparsers.add_parser("disconnect", help="Disconnect the device")
disconnect_subparser.add_argument("DevEUI", type=str, help="DevEUI of the device")

# Ignore
ignore_subparser = subparsers.add_parser("ignore", help="Ignore the device")
ignore_subparser.add_argument("DevEUI", type=str, help="DevEUI of the device")
ignore_subparser.add_argument(
    "-r", "--revert", help="Revert the ignore", action="store_true"
)

# List devices
list_subparser = subparsers.add_parser("list", help="List connected devices")
list_subparser.set_defaults(func=list_devices)

# Device info
dev_subparser = subparsers.add_parser("dev", help="Show information about the device")
dev_subparser.add_argument("DevEUI", type=str, help="DevEUI of the device")

# Validate
validate_subparser = subparsers.add_parser(
    "validate", help="Validate the automation file"
)
validate_subparser.add_argument(
    "filepath", type=str, help="Path to the automation file"
)

# Apply
apply_subparser = subparsers.add_parser("apply", help="Apply the automation file")
apply_subparser.add_argument("filepath", type=str, help="Path to the automation file")

args = parser.parse_args()
print(args)

print(args.func(args.DevEUI))


with (
    open("examples/mvp-normalized.yaml", "r") as yaml_in,
    open("examples/mvp_test.json", "w") as json_out,
):
    yaml_object = yaml.safe_load(yaml_in)  # yaml_object will be a list or a dict
    json.dump(yaml_object, json_out)
