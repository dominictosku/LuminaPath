<template>
    <nav class="border-4 border-gray-800 box-shadow bg-gradient-to-r from-zinc-800 to-slate-400">
        <div class="container mx-auto sm:px-4">
            <div>
                <ul class="sm:flex sm:flex-wrap pl-0 mb-0 gap-8">
                    <li class="max-sm:border-2 border-black">
                        <div>
                            <div class="link-container">
                                <a class="link-two menuLinks text-4xl" href="">Games</a>
                            </div>
                        </div>
                    </li>
                    <li class="max-sm:border-2 border-black">
                        <div class="link-container">
                            <a class="link-two menuLinks text-4xl" href="">Anime</a>
                        </div>
                    </li>
                </ul>
            </div>
        </div>
    </nav>
    </template>
    <style scoped>    
    /*Credits to https://jhancock532.github.io/link-hover-effects/ */
    .link-container {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        height: 100%;
        position: relative;
        z-index: 0;
    }
    
    .menuLinks {
        font-family: "Bungee", cursive;
        text-decoration: none;
    }
    
    .link-two {
        color: white;
        transition: color 1s cubic-bezier(0.32, 0, 0.67, 0);
    }
    
        .link-two:hover {
            color: black;
            transition: color 1s cubic-bezier(0.33, 1, 0.68, 1);
        }
    
        .link-two::before {
            content: "";
            position: absolute;
            z-index: -2;
            width: 100%;
            height: 100%;
            top: 0;
            right: 0;
            clip-path: polygon( 0% -20%, 100% -30%, 100% -10%, 0% 0%, 0% 130%, 100% 120%, 100% 100%, 0% 110% );
            background-color: azure;
            transition: clip-path 1s cubic-bezier(0.25, 1, 0.5, 1);
        }
    
        .link-two:hover::before {
            clip-path: polygon( 0% 10%, 100% 0%, 100% 20%, 0% 30%, 0% 100%, 100% 90%, 100% 70%, 0% 80% );
        }
    
        .link-two::after {
            content: "";
            position: absolute;
            z-index: -1;
            width: 5ch;
            height: 5ch;
            top: 50%;
            right: 50%;
            transform: translate(50%, -50%) rotate(0deg) scale(0);
            transition: transform 1s ease;
            background-color: cornflowerblue;
        }
    
        .link-two:hover::after {
            transform: translate(50%, -50%) rotate(135deg) scale(1);
        }
    
    </style>