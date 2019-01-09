**The Azure IoT SDKs team wants to hear from you!**

- [Need support?](#need-support)
- [Contribute code or documentation](#contribute-code-or-documentation)

## Need Support?
* Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
* Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”
* Need Support? Every customer with an active Azure subscription has access to support with guaranteed response time.  Consider submitting a ticket and get assistance from Microsoft support team
* Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub (C, Java, .NET, Node.js, Python).

## Contribute code or documentation
Our SDKs are open-source and we do accept pull-requests if you feel like taking a stab at fixing the bug and maybe adding your name to our commit history :) Please mention any relevant issue number in the pull request description, and follow the contributing guidelines [below](#contributing-guidelines).

We require pull-requests for code and documentation to be submitted against the `master` branch in order to review and run it in our gated build system. We try to maintain a high bar for code quality and maintainability, we insist on having tests associated with the code, and if necessary, additions/modifications to the requirement documents.

#### Contributing guidelines:
1. If the change affects the public API, extract the updated public API surface and submit a PR for review. Make sure you get a signoff before you move to Step 2.
2. Post API surface approval, follow the below guidelines for contributing code:
    1. Follow the steps [here](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/doc/devbox_setup.md) for setting up your development environment.
    2. Follow the [C# Coding Style](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/doc/coding-style.md).
    2. Unit Tests:
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
    3. E2E Tests:
    Any new feature or functionality added must have associated end-to-end tests.
        1. Update/ Add the E2E tests [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/e2e/test).
        2. In case environmental setup required for the application is changed, update the pre-requisites [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/e2e/test/prerequisites).
        3. Run the E2E test suite and ensure that all the tests pass successfully. You can also test against the [CI script](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/jenkins/windows_csharp.cmd) that is used in our gated build system.
    4. Samples:
    Add relevant samples to the [Azure IoT Samples for C# Repo](https://github.com/Azure-Samples/azure-iot-samples-csharp). Make sure to add a supporting readme file demonstrating the steps to run the sample.
    5. Documentation:
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
3. Post completion of all of the above steps, create a PR against `master`.

Also, have you signed the [Contribution License Agreement](https://cla.microsoft.com/) ([CLA](https://cla.microsoft.com/))? A friendly bot will remind you about it when you submit your pull-request.

**If your contribution is going to be a major effort, you should give us a heads-up first. We have a lot of items captured in our backlog and we release every two weeks, so before you spend the time, just check with us to make sure your plans and ours are in sync :) Just open an issue on GitHub and tag it as "contribution".**

## Review Process
We expect all guidelines to be met before accepting a pull request. As such, we will work with you to address issues we find by leaving comments in your code. Please understand that it may take a few iterations before the code is accepted as we maintain high standards on code quality. Once we feel comfortable with a contribution, we will validate the change and accept the pull request.

Thank you for any contributions! Please let the team know if you have any questions or concerns about our contribution policy.
