# ArmoniK.Contrib.Plugin.SimpleAmqp

This repository shows a use case of using a queue adaptor as a plugin to [ArmoniK.Core](https://github.com/aneoconsulting/ArmoniK.Core). The idea is to have an external adaptor, and to plug this adaptor to ArmoniK.Core. The methodology for using external adaptors is described in [Using external plugins with ArmoniK.Core](https://github.com/aneoconsulting/ArmoniK.Core/blob/main/.docs/content/1.concepts/10.plugins.md).

The adaptor provided in this repository consists of an example of a simple Amqp adaptor. You are provided the adaptor implementation as well as the docker files that allow to pack it as a plugin.

The two docker files allow to build new images of the polling agent and the control plane containing this plugin. These images are based on polling agent and control plane existing images and add this plugin on top.

Clone this repository, place yourself at the root and build the images using:

```shell
docker build --build-arg IMAGE=dockerhubaneo/armonik_pollingagent:0.17.0 -t  pollingagentplugin:0.17.0 -f ./PollingAgent/Dockerfile .
docker build --build-arg IMAGE=dockerhubaneo/armonik_control:0.17.0 -t submitterplugin:0.17.0 -f ./Submitter/Dockerfile .
```

The `IMAGE` argument serves to pass the base image as command line argument.

The next step is to use these images in the deployment of ArmoniK.Core. First, clone the repository:

```shell
git clone https://github.com/aneoconsulting/ArmoniK.Core.git.
```

Next, check [the documentation](https://github.com/aneoconsulting/armonik.core) to make sure you have all the requirements needed to deploy Core.

Then you will need to change two variables in order to use the images you built with your plugin, not default images:

```shell
export POLLING_AGENT_IMAGE="pollingagentplugin"
export SUBMITTER_IMAGE="submitterplugin"
```

A last step before deploying Core is to pass needed configuration variables using `TF_VAR_custom_env_vars`:

```shell
export TF_VAR_custom_env_vars='{ "Components__QueueAdaptorSettings__ClassName" = "ArmoniK.Contrib.Plugin.SimpleAmqp.QueueBuilder", "Components__QueueAdaptorSettings__AdapterAbsolutePath" = "/adapters/queue/simpleamqp/ArmoniK.Contrib.Plugin.SimpleAmqp.dll" }'
```

Finally, you can deploy Core:

```shell
just tag=0.17.0 deploy
```

It will be deployed using the two images of polling agent and submitter that you changed and it will take into consideration the configuration of the plugin that you provided.
