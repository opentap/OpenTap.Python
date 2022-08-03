module.exports = {
    title: 'OpenTAP Python Integration',
    description: 'Python Integration for OpenTAP',
    
    themeConfig: {
        repo: 'https://github.com/opentap/OpenTap.Python',
        editLinks: true,
        editLinkText: 'Help improve this page!',
        docsBranch: 'dev',
        docsDir: 'Documentation',
        nav: [
            { text: 'OpenTAP', link: 'https://github.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['/', "Welcome"],
            ['TAP_Python_Help/', 'OpenTAP Python'],
            ['TAP_Python_Help/Prerequisites.md', 'Prerequisites'],
            [ 'TAP_Python_Help/Getting_Started.md', 'Getting Started'],
            [ 'TAP_Python_Help/Code_Examples.md', 'Example Code'],
            [ 'TAP_Python_Help/Debugging_with_Microsoft_Visual_Studio.md', 'Debugging on Visual Studio'],
            ['TAP_Python_Help/Limitations.md', 'Limitations'],
            {
                title: "Release Notes",
                children:
                [
                    ['Release_Notes/ReleaseNotes_1_1.md', "Version 1.1.0"],
                    ['Release_Notes/ReleaseNotes_2_0.md', "Version 2.0.0"],
                    ['Release_Notes/ReleaseNotes_2_3.md', "Version 2.3.0"],
                    ['Release_Notes/ReleaseNotes_2_3_1.md', "Version 2.3.1"],
                ]
            }
        ]
    },
    dest: '../public',
    base: '/OpenTap.Python/'
}

