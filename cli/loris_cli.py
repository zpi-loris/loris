import argparse
import yaml
import json

parser = argparse.ArgumentParser(
    prog="loris CLI",
    description="Use this tool to communicate with the loris client app via CLI",
)

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

parser.add_argument("-p", "--parse-automation")

args = parser.parse_args()

print(args)

with (
    open("examples/mvp-normalized.yaml", "r") as yaml_in,
    open("examples/mvp_test.json", "w") as json_out,
):
    yaml_object = yaml.safe_load(yaml_in)  # yaml_object will be a list or a dict
    json.dump(yaml_object, json_out)
