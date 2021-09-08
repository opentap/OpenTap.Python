module.exports = {
    title: 'OpenTAP Python Integration',
    description: 'Python Integration for OpenTAP',
    
    themeConfig: {
        repo: 'https://gitlab.com/opentap/plugins/Python',
        editLinks: true,
        editLinkText: 'Help improve this page!',
        docsDir: 'Documentation',
        nav: [
            { text: 'OpenTAP', link: 'https://gitlab.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['/', "Welcome"],
            ['TAP_Python_Help/', 'OpenTAP Python'],
            ['TAP_Python_Help/Prerequisites.md', 'Prerequisites'],
            {
                title: "Getting Started",
                children:[
                    'TAP_Python_Help/Python_Development_Examples/',
                    ['TAP_Python_Help/Python_Development_Examples/Building_the_Python_Examples_for_Windows.md', 'Building the Examples on Windows'],
                    ['TAP_Python_Help/Python_Development_Examples/Building_the_Python_Examples_for_Ubuntu.md', 'Building the Examples on Ubuntu'],
                    ['TAP_Python_Help/Python_Development_Examples/Create_and_Run_a_Simple_Test_Plan_for_Windows.md', 'Create and Run a Simple Test Plan on Windows'],
                    ['TAP_Python_Help/Python_Development_Examples/Create_and_Run_a_Simple_Test_Plan_for_Ubuntu.md', 'Create and Run a Simple Test Plan on Ubuntu']
                ]
            },
            {
                title: "Creating a Plugin",
                children:
                    [
                        [ 'TAP_Python_Help/Project_Creation_Wizard.md', 'Project Creation Wizard' ],
                        [ 'TAP_Python_Help/Creating_a_plugin_with_Python_for_Windows.md', 'Windows'],
                        [ 'TAP_Python_Help/Creating_a_plugin_with_Python_for_Ubuntu.md', 'Ubuntu']
                    ]
            },
            [ 'TAP_Python_Help/Code_Examples.md', 'Example Code'],
            [ 'TAP_Python_Help/Debugging_with_Microsoft_Visual_Studio.md', 'Debugging on Visual Studio'],
            ['TAP_Python_Help/Limitations.md', 'Limitations'],
            {
                title: "Release Notes",
                children:
                [
                    ['Release_Notes/ReleaseNotes_1_1.md', "Version 1.1"],
                    ['Release_Notes/ReleaseNotes_2_0.md', "Version 2.0"]
                ]
            }
        ]
    },
    dest: '../public',
    base: '/Plugins/python/'
}

