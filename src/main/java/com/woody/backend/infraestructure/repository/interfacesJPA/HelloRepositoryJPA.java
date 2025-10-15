package com.woody.backend.infraestructure.repository.interfacesJPA;

import org.springframework.data.jpa.repository.JpaRepository;

import com.woody.backend.infraestructure.entity.Hello;

public interface HelloRepositoryJPA extends JpaRepository<Hello, String> {
    
}
