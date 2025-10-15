package com.woody.backend.infraestructure;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Repository;

import com.woody.backend.infraestructure.entity.Hello;
import com.woody.backend.infraestructure.repository.interfaces.HelloRepository;
import com.woody.backend.infraestructure.repository.interfacesJPA.HelloRepositoryJPA;

@Repository
public class HelloImplRepository implements HelloRepository{
    @Autowired
    private HelloRepositoryJPA helloRepo;

    public HelloImplRepository(){}

    @Override
    public List<Hello> get(){
        return this.helloRepo.findAll();
    }
}
