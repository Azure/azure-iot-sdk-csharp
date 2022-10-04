**The Azure IoT SDKs team wants to hear from you!**

- [Ask a question](#ask-a-question)
- [File a bug](#file-a-bug-(code-or-documentation-))
- [Need support?](#need-support)
- [Contribute documentation](#contribute-documentation)
- [Contribute code](#contribute-code)
- [Contributing guidelines](#Contributing-guidelines)

# Ask a question
Our team monitors Stack Overflow, especially the [azure-iot-hub](http://stackoverflow.com/questions/tagged/azure-iot-hub) tag. It really is the best place to ask.

We monitor the Github issues section specifically for bugs found with our SDK, however we will reply to questions asked using Github issues too.

# File a bug (code or documentation)
That is definitely something we want to hear about. Please open an issue on github, we'll address it as fast as possible. Typically here's the information we're going to ask for to get started:
- What SDK are you using (Node, C, C#, Python, Java?)
- What version of the SDK?
- Do you have a snippet of code that would help us reproduce the bug?
- Do you have logs showing what's happening?

## Need Support?
* Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
* Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”
* Need Support? Every customer with an active Azure subscription has access to support with guaranteed response time.  Consider submitting a ticket and get assistance from Microsoft support team
* Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub. This is the repo for C# only. Use the respective repo for (C, Java, .NET, Node.js, Python).

## Contribute documentation

For simple markdown files, we accept documentation pull requests submitted against the `main` branch, if it's about existing SDK features.
If your PR is about future changes or has changes to the comments in the code itself, we'll treat is as a code change (see the next section).

## Contribute code
Our SDKs are open-source and we do accept pull-requests if you feel like taking a stab at fixing the bug and maybe adding your name to our commit history :) Please mention any relevant issue number in the pull request description, and follow the contributing guidelines [below](#contributing-guidelines).

Pull-requests for code are to be submitted against the `main` branch. We will review the request and once approved we will be running it in our gated build system. We try to maintain a high bar for code quality and maintainability, we insist on having tests associated with the code, and if necessary, additions/modifications to the requirement documents.

Also, have you signed the [Contribution License Agreement](https://cla.microsoft.com/) ([CLA](https://cla.microsoft.com/))? A friendly bot will remind you about it when you submit your pull-request.

If you feel like your contribution is going to be a major effort, you should probably give us a heads-up. We have a lot of items captured in our backlog and we release every two weeks, so before you spend the time, just check with us to make
sure your plans and ours are in sync :) Just open an issue on github and tag it "enhancement" or "feature request"

## Contributing guidelines
1. If the change affects the public API, extract the updated public API surface and submit a PR for review. Make sure you get a signoff before you move to Step 2.
2. Post API surface approval, follow the below guidelines for contributing code:

    a) Follow the steps [here](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/doc/devbox_setup.md) for setting up your development environment.

    b) Follow the [C# Coding Style](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/doc/coding-style.md).

    c) Unit Tests:
    We write unit tests for any new function or block of application code that impacts the existing behavior of the code.
    ```
    public class Foo
    {
        public bool IsEven(int x)
        {
            return (x%2 == 0);
        }
    }

    [TestClass]
    public class FooTests
    {
        [TestMethod]
        public void Foo_IsEven_EvenNumReturnsTrue()
        {
            var foo = new Foo();
            bool result = foo.IsEven(10);
            Assert.IsTrue(result);
        }
    }
    ```
    d) E2E Tests:
    Any new feature or functionality added must have associated end-to-end tests.
        1. Update/ Add the E2E tests [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/e2e/test).
        2. In case environmental setup required for the application is changed, update the pre-requisites [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/e2e/test/prerequisites).
        3. Run the E2E test suite and ensure that all the tests pass successfully. You can also test against the [CI script](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/jenkins/windows_csharp.cmd) that is used in our gated build system.

    e) Samples:
    Add relevant samples to the [Azure IoT Samples for C# Repo](https://github.com/Azure-Samples/azure-iot-samples-csharp). Make sure to add a supporting readme file demonstrating the steps to run the sample.
    
    f) Documentation:
    To make sure that the API documentation can be generated from our code, we follow the following format for class and method signatures:
    ```
    /// <summary>
    /// Short description here
    /// </summary>
    public class Foo
    {
        /// <summary>
        /// Short description here
        /// </summary>
        /// <param name="param1"> Description for param1. </param>
        /// <param name="param2"> Description for param2. </param>
        /// <returns> Description for the return value. </returns>
        public int Bar(int param1, int param2)
        {
            return 0;
        }
    }
    ```
3. Post completion of all of the above steps, create a PR against `main`.

#### Commit Guidelines
We have very precise rules over how our git commit messages can be formatted. This leads to more readable messages that are easy to follow when looking through the project history.

#### Commit Message Format

Each commit message consists of a header, a body (optional) and a footer (optional). The header has a special format that includes a type, a scope and a subject:

```C#

<type>(<scope>): <subject>
<BLANK LINE>
<body>
<BLANK LINE>
<footer>

```
The header is mandatory and the body and footer are optional.

Any line of the commit message cannot be longer 100 characters! This allows the message to be easier to read on GitHub as well as in various git tools.

Footer should contain a [closing reference](#https://help.github.com/articles/closing-issues-using-keywords/) to an issue if any.

**Revert**

If the commit reverts a previous commit, it should begin with `revert:`, followed by the header of the reverted commit. In the body it should say: `This reverts commit <hash>.`, where the hash is the SHA of the commit being reverted.

**Rebase and Squash**

* Its manadatory to squash all your commits per scope (i.e package). It is also important to rebase your commits on `main`.
* Optionally you can split your commits on the basis of the package you are providing code to.

**Type**

Must be one of the following:

* build: Changes that affect the build system
* docs: Documentation only changes (eg ReadMe)
* feat: A new feature
* fix: A bug fix
* perf: A code change that improves performance
* refactor: A code change that neither fixes a bug nor adds a feature

Optionally you could also use the following scope:

* style: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
* test: Adding missing tests or correcting existing tests

**Scope**

The scope should be the name of the package affected as perceived by person reading changelog generated from commit messages.

    * shared
    * iothub-client
    * iothub-service
    * provisioning-client
    * provisioning-service
    * provisioning-transport-amqp
    * provisioning-transport-mqtt
    * provisioning-transport-http
    * pnp-device
    * pnp-service
    * e2e
    * github

**Subject**

The subject contains succinct description of the change:

* use the imperative, present tense: "change" not "changed" nor "changes"
* no dot (.) at the end

**Body**

Just as in the subject, use the imperative, present tense: 

"change" not "changed" nor "changes". The body should include the motivation for the change and contrast this with previous behavior.

**Footer**

The footer should contain any information about Breaking Changes and is also the place to reference GitHub issues that this commit Closes.

Breaking Changes should start with the word BREAKING CHANGE: with a space or two newlines. The rest of the commit message is then used for this.

***Example commit messages***

Good commit messages look like below:

* fix(devices-client, shared): Fix failure in reconnection

    Fix failure in MQTT reconnection when token expires.

    Github issue (fix#123)
* feat(service-client): Add continuation token to Query
* docs: Update readme to reflect provisiong client

Bad commit messages look like below:

* fix(service-client): small fix

    I was trying to reconnect and network dropped. Fixing such random failures

* feat(devices-client): add test
* docs: update readme 

References for commit guidelines 
* https://udacity.github.io/git-styleguide/
* https://github.com/angular/angular/blob/master/CONTRIBUTING.md#-commit-message-guidelines
* https://github.com/googlesamples/android-architecture/issues/300

## Review Process
We expect all guidelines to be met before accepting a pull request. As such, we will work with you to address issues we find by leaving comments in your code. Please understand that it may take a few iterations before the code is accepted as we maintain high standards on code quality. Once we feel comfortable with a contribution, we will validate the change and accept the pull request.

Thank you for any contributions! Please let the team know if you have any questions or concerns about our contribution policy.
