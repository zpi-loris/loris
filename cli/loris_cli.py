from pprint import pprint
import sys
import re
import argparse
import yaml
import json
import requests

parser = argparse.ArgumentParser(
    prog="loris",
    description="Use this tool to communicate with the running loris client.",
)

base_url = "http://localhost:5158/api"
# opts
# - parse automation
#   - validate (sth like terraform plan)
#   - apply
# - view capabilities of the device
# - list devices
# - disconnect device
# - welcome the device
# - ignore(revert) the device


def convert_yaml(yaml_object):
    for automation in yaml_object:
        for command, a_values in automation.items():
            if command in ("triggers", "conditions", "actions"):
                new_params = []
                for c_values in a_values:
                    for c_type, params in c_values.items():
                        params = {"$type": c_type, **params}
                        new_params.append(params)
                automation[command] = new_params

    with open("saved.yaml", "w") as f:
        yaml.safe_dump(yaml_object, f, sort_keys=False)


def print_invalid_rule(invalid_rule):
    print(f"Invalid automation: {invalid_rule.get('name')}", file=sys.stderr)
    print(f"Automation ID: {invalid_rule.get('id')}", file=sys.stderr)

    errors = invalid_rule.get("errors")

    for i, err in enumerate(errors, 1):
        pointer = err.get("pointer")
        value = err.get("value")
        details = err.get("details")

        print(f"     {i}. Field:  {pointer}", file=sys.stderr)
        print(f"        Value:  '{value}'", file=sys.stderr)
        print(f"        Detail: {details}", file=sys.stderr)


def print_syntax_error(error):
    code = list(error.keys())[1]
    print(code)
    pattern = r"\$\[(\d+)\]\.(\w+)\[(\d+)\]"

    match = re.search(pattern, code)

    if match:
        automation_idx = int(match.group(1))
        command = match.group(2).strip()
        command_idx = int(match.group(3))

        print(
            f"Syntax error in automation number {automation_idx}, in command {command}, block {command_idx}",
            file=sys.stderr,
        )


def accept(args):
    headers = {"Accept": "application/json"}
    if args.by_eui:
        request_url = f"{base_url}/devices/by-eui/{args.device}/accept"
    else:
        request_url = f"{base_url}/devices/by-name/{args.device}/accept"

    r = requests.post(request_url, headers=headers)

    if not r.ok:
        try:
            details = r.json().get("detail")
            print(
                f"Request failed with status code {r.status_code}: {details}",
                file=sys.stderr,
            )
        except (ValueError, AttributeError):
            print(f"Request failed with status code {r.status_code}.", file=sys.stderr)

        sys.exit(1)

    print(f"Device {args.device} accepted successfully!")


def disconnect(args):
    headers = {"Accept": "application/json"}
    if args.by_eui:
        request_url = f"{base_url}/devices/by-eui/{args.device}/disconnect"
    else:
        request_url = f"{base_url}/devices/by-name/{args.device}/disconnect"

    if args.force:
        r = requests.post(request_url, params="force=true", headers=headers)
    else:
        r = requests.post(request_url, headers=headers)

    if not r.ok:
        try:
            details = r.json().get("detail")
            print(
                f"Request failed with status code {r.status_code}: {details}",
                file=sys.stderr,
            )
        except (ValueError, AttributeError):
            print(f"Request failed with status code {r.status_code}.", file=sys.stderr)

        sys.exit(1)

    print(f"Device {args.device} disconnected")


def ignore(args):
    headers = {"Accept": "application/json"}
    if args.by_eui:
        if args.revert:
            request_url = f"{base_url}/devices/by-eui/{args.device}/ignore"
            r = requests.post(request_url, params="revert=true", headers=headers)
        else:
            request_url = f"{base_url}/devices/by-eui/{args.device}/ignore"
            r = requests.post(request_url, headers=headers)
    else:
        if args.revert:
            request_url = f"{base_url}/devices/by-name/{args.device}/ignore"
            r = requests.post(request_url, params="revert=true", headers=headers)
        else:
            request_url = f"{base_url}/devices/by-name/{args.device}/ignore"
            r = requests.post(request_url, headers=headers)

    if not r.ok:
        try:
            details = r.json().get("detail")
            print(
                f"Request failed with status code {r.status_code}: {details}",
                file=sys.stderr,
            )
        except (ValueError, AttributeError):
            print(f"Request failed with status code {r.status_code}.", file=sys.stderr)

        sys.exit(1)

    if args.revert:
        print(f"Device {args.device} is no longer being ignored")
    else:
        print(f"Device {args.device} ignored")


# def list_devices():
#     request_url = f"{base_url}/devices/by-eui/{args.device}/accept"
#
#     if args.pending:
#         pass
#     elif args.accepted:
#         pass
#     elif args.ignored:
#         pass
#     elif args.blocked:
#         pass
#     else:
#         pass
#
#     r = requests.post(request_url)
#     if r.status_code != 200:
#         print(f"ERROR: Status Code {r.status_code}")
#         return
#
#
# def device(args):
#     request_url = f"{base_url}/devices/by-eui/{args.device}/accept"
#
#     r = requests.post(request_url)
#     if r.status_code != 200:
#         print(f"ERROR: Status Code {r.status_code}")
#         return
#     print(r.content)


def apply(args):
    headers = {"Accept": "application/json", "Content-Type": "application/json"}
    request_url = f"{base_url}/automations/apply"

    with open(args.filepath, "r") as yaml_in:
        yaml_object = yaml.safe_load(yaml_in)
        convert_yaml(yaml_object)

    # with open("saved.json", "w") as f:
    #    json.dump(yaml_object, f, indent=2, default=dict)

    json_string = json.dumps(yaml_object, default=str)

    r = requests.post(request_url, data=json_string, headers=headers)
    if not r.ok:
        try:
            print(f"Request failed with status code {r.status_code}", file=sys.stderr)
            data = r.json()

            if "invalidRules" in data:
                for invalid_rule in data["invalidRules"]:
                    print_invalid_rule(invalid_rule)
            elif "errors" in data:
                print_syntax_error(data["errors"])
        except (ValueError, AttributeError):
            print(f"Request failed with status code {r.status_code}.", file=sys.stderr)
        sys.exit(1)

    print("Automation applied successfully!")


def validate(args):
    headers = {"Accept": "application/json", "Content-Type": "application/json"}
    request_url = f"{base_url}/automations/validate"

    with open(args.filepath, "r") as yaml_in:
        yaml_object = yaml.safe_load(yaml_in)
        convert_yaml(yaml_object)

    # with open("saved.json", "w") as f:
    #    json.dump(yaml_object, f, indent=2, default=dict)

    json_string = json.dumps(yaml_object, default=str)

    r = requests.post(request_url, data=json_string, headers=headers)
    if not r.ok:
        try:
            print(f"Request failed with status code {r.status_code}", file=sys.stderr)
            data = r.json()

            if "invalidRules" in data:
                for invalid_rule in data["invalidRules"]:
                    print_invalid_rule(invalid_rule)
            elif "errors" in data:
                print_syntax_error(data["errors"])
        except (ValueError, AttributeError):
            print(f"Request failed with status code {r.status_code}.", file=sys.stderr)
        sys.exit(1)

    print("Validation was successful!")


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
disconnect_subparser.add_argument(
    "device", type=str, help="Name (default) or the EUI (-d option) of the device"
)
disconnect_subparser.add_argument(
    "-d", "--by-eui", help="Accept by the EUI of the device", action="store_true"
)
disconnect_subparser.add_argument(
    "-f", "--force", help="Force disconnect", action="store_true"
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
# list_subparser.set_defaults(func=list_devices)
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
# list_subparser.set_defaults(func=list_devices)

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

if "func" in args:
    try:
        args.func(args)
    except requests.exceptions.ConnectionError:
        print("Something went wrong. Check if the given address is correct.")
else:
    parser.print_help()
