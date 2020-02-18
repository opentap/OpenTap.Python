module.exports = {
    title: 'OpenTAP Python Integration',
    description: 'Python Integration for OpenTAP',
    
    themeConfig: {
        repo: 'https://gitlab.com/romadsen-ks/Python',
        editLinks: true,
        editLinkText: 'Help improve this page!',
        docsDir: 'Documentation',
        nav: [
            { text: 'OpenTAP', link: 'https://gitlab.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['/', "Welcome"],
            ['TAP Python Help/', 'OpenTAP Python'],
            ['TAP Python Help/Prerequisites.md', 'Prerequisites'],
            {
                title: "Getting Started",
                children:[
                    'TAP Python Help/Python Development Examples/',
                    ['TAP Python Help/Python Development Examples/Building the Python Examples for Windows.md', 'Building the Examples on Windows'],
                    ['TAP Python Help/Python Development Examples/Building the Python Examples for Ubuntu.md', 'Building the Examples on Ubuntu'],
                    ['TAP Python Help/Python Development Examples/Create and Run a Simple Test Plan for Windows.md', 'Create and Run a Simple Test Plan on Windows'],
                    ['TAP Python Help/Python Development Examples/Create and Run a Simple Test Plan for Ubuntu.md', 'Create and Run a Simple Test Plan on Ubuntu']
                ]
            },
            {
                title: "Creating a Plugin",
                children:
                    [
                        [ 'TAP Python Help/Creating a Plugin with Python for Windows.md', 'Windows'],
                        [ 'TAP Python Help/Creating a plugin with Python for Ubuntu.md', 'Ubuntu']
                    ]
            },
            [ 'TAP Python Help/Code Examples.md', 'Example Code'],
            [ 'TAP Python Help/Debugging_with_Microsoft_Visual_Studio.md', 'Debugging on Visual Studio'],
            {
                title: "Release Notes",
                children:
                [
                    ['Release Notes/ReleaseNotes_1_1.md', "Version 1.1"],
                    ['Release Notes/ReleaseNotes_2_0.md', "Version 2.0"]
                ]
            },
            ['TAP Python Help/Notices.md', 'Notices']
        ]
    },
    dest: '../public',
    base: '/Plugins/python/'
}

